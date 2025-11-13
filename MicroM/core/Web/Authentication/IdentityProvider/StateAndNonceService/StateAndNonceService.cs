using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace MicroM.Web.Authentication.SSO;

public class StateAndNonceService
    (
    IHttpContextAccessor http_context,
    IMicroMAppConfiguration app_config,
    ILogger<StateAndNonceService> log
    ) : IStateAndNonceService
{

    private const int STATE_COOKIE_TTL_MINUTES = 10;

    private static string GetStateAndNonceCookieName(ApplicationOption app_config)
    {
        return $"m-oidc-stn-{app_config.ApplicationID}";
    }

    public StateAndNonceContext EnsureStateAndNonce(IFormCollection original, string? providedState, string? providedNonce, string? providedDeviceId)
    {
        string state = string.IsNullOrWhiteSpace(providedState)
            ? CryptClass.GenerateBase64UrlRandomCode(32)
            : providedState;

        // Changed: nonce now base64url random
        string nonce = string.IsNullOrWhiteSpace(providedNonce)
            ? CryptClass.GenerateBase64UrlRandomCode(32)
            : providedNonce;

        string? deviceId = !string.IsNullOrWhiteSpace(providedDeviceId)
            ? providedDeviceId
            : (original.TryGetValue(WellknownIdentityConstants.LocalDeviceId, out var ldid) ? ldid.ToString() : null);

        var dict = new Dictionary<string, StringValues>(StringComparer.Ordinal);
        foreach (var kv in original) dict[kv.Key] = kv.Value;
        dict[WellknownIdentityConstants.State] = new(state);
        dict[WellknownIdentityConstants.Nonce] = new(nonce);
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            dict[WellknownIdentityConstants.LocalDeviceId] = new(deviceId);
        }

        var data = new StateAndNonceData(state, nonce, deviceId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        return new StateAndNonceContext(data, new FormCollection(dict));
    }

    public void StoreStateCookie(ApplicationOption app, string hmacKey, StateAndNonceData data)
    {
        var context = http_context.HttpContext ?? throw new InvalidOperationException("No HttpContext available");

        var encoded = StateAndNonceHashed.Encode(hmacKey, data);

        var cookie_name = GetStateAndNonceCookieName(app);
        var response = context.Response;
        var app_tenant_path = app_config.GetTenantPath(context);

        response.Cookies.Append(
            cookie_name,
            encoded,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = app_tenant_path,
                Expires = DateTimeOffset.UtcNow.AddMinutes(STATE_COOKIE_TTL_MINUTES)
            });
    }

    public ResultWithStatus<StateAndNonceData, string> ValidateAndConsumeStateCookie(string app_id, string hmacKey, string incomingState)
    {
        if (string.IsNullOrWhiteSpace(incomingState))
        {
            return new(null, "state_missing");
        }

        var context = http_context.HttpContext ?? throw new InvalidOperationException("No HttpContext available");
        var request = context.Request;

        var appCfg = app_config.GetAppConfiguration(app_id) ?? throw new InvalidOperationException("App config not found");
        var cookieName = GetStateAndNonceCookieName(appCfg);

        if (!request.Cookies.TryGetValue(cookieName, out var stored))
        {
            log.LogDebug("State cookie {cookie_name} missing", cookieName);
            return new(null, "state_cookie_missing");
        }

        var (hashed, error) = StateAndNonceHashed.Decode(hmacKey, stored);
        if (error != null || hashed == null)
        {
            log.LogDebug("State cookie {cookie_name} decode error: {error}", cookieName, error);
            return new(null, error);
        }

        if (hashed.IsExpired(STATE_COOKIE_TTL_MINUTES))
        {
            log.LogDebug("State cookie {cookie_name} expired", cookieName);
            return new(null, "state_cookie_expired");
        }

        if (!string.Equals(hashed.Data.State, incomingState, StringComparison.Ordinal))
        {
            log.LogDebug("State cookie {cookie_name} state mismatch (expected {expected}, got {actual})", cookieName, hashed.Data.State, incomingState);
            return new(null, "state_mismatch");
        }

        // Single-use
        context.Response.Cookies.Delete(cookieName, new CookieOptions { Path = app_config.GetTenantPath(context) });

        return new(hashed.Data, null);
    }
}
