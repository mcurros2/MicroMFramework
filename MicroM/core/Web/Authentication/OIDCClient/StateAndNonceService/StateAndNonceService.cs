using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

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

    public StateAndNonceContext CreateStateNonceAndPkce(
        IFormCollection original,
        string? providedDeviceId,
        OIDCCodeChallengeMethod codeChallengeMethod,
        string? targetLinkUri
        )
    {

        // Generate cryptographically strong state / nonce
        string state = CryptClass.GenerateBase64UrlRandomCode(32);
        string nonce = CryptClass.GenerateBase64UrlRandomCode(32);

        // Generate PKCE verifier
        string codeVerifier = CryptClass.GenerateBase64UrlRandomCode(64);

        // Compute challenge
        string codeChallenge;
        if (codeChallengeMethod == OIDCCodeChallengeMethod.S256)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(codeVerifier);
            var hash = SHA256.HashData(bytes);
            codeChallenge = Base64UrlEncoder.Encode(hash);
        }
        else
        {
            codeChallenge = codeVerifier;
        }

        string? deviceId = !string.IsNullOrWhiteSpace(providedDeviceId)
            ? providedDeviceId
            : (original.TryGetValue(WellknownIdentityConstants.LocalDeviceId, out var ldid) ? ldid.ToString() : null);


        var dict = new Dictionary<string, StringValues>(StringComparer.Ordinal)
        {
            [WellknownIdentityConstants.State] = state,
            [WellknownIdentityConstants.Nonce] = nonce,
            [WellknownIdentityConstants.CodeChallenge] = codeChallenge,
            [WellknownIdentityConstants.CodeChallengeMethod] = codeChallengeMethod.ToString()
        };

        if (!string.IsNullOrEmpty(targetLinkUri)) dict[WellknownIdentityConstants.TargetLinkUri] = targetLinkUri;

        if (!string.IsNullOrWhiteSpace(deviceId)) dict[WellknownIdentityConstants.LocalDeviceId] = deviceId;

        var data = new StateAndNonceData(state, nonce, deviceId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), codeVerifier, targetLinkUri);
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

        if (hashed.Data.State != incomingState)
        {
            log.LogDebug("State cookie {cookie_name} state mismatch (expected {expected}, got {actual})", cookieName, hashed.Data.State, incomingState);
            return new(null, "state_mismatch");
        }

        // Single-use
        context.Response.Cookies.Delete(cookieName, new CookieOptions { Path = app_config.GetTenantPath(context) });

        return new(hashed.Data, null);
    }
}
