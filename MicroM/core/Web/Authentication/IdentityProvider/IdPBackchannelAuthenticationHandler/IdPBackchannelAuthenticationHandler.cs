using MicroM.Configuration;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace MicroM.Web.Authentication.SSO;

/// <summary>
/// Authentication handler that accepts either:
///  - HTTP Basic auth (client_id:client_secret) where client_secret is stored in app.OIDCClientConfiguration[client_id].APISecret
///  - private_key_jwt: client_assertion (JWT) in form body validated against client's JWKS (URLClientJWKS)
/// The handler sets HttpContext.User.Identity.Name = client_id and issues a ClaimTypes.NameIdentifier claim with client_id.
/// </summary>
public class IdPBackchannelAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IMicroMAppConfiguration _appConfig;
    private readonly ILogger<IdPBackchannelAuthenticationHandler> _log;
    private readonly IIdPClientSigningKeysCacheService _clientSigningKeysCache;

    public IdPBackchannelAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IMicroMAppConfiguration appConfig,
        IIdPClientSigningKeysCacheService clientSigningKeysCache
        ) : base(options, logger, encoder)
    {
        _appConfig = appConfig;
        _log = logger.CreateLogger<IdPBackchannelAuthenticationHandler>();
        _clientSigningKeysCache = clientSigningKeysCache;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string app_id = Context.Request.RouteValues["app_id"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(app_id))
        {
            _log.LogDebug("IdP backchannel auth: missing app_id route value");
            return AuthenticateResult.NoResult();
        }

        var app = _appConfig.GetAppConfiguration(app_id);
        if (app == null)
        {
            _log.LogWarning("IdP backchannel auth: unknown app_id {appId}", app_id);
            return AuthenticateResult.Fail("Unknown app");
        }

        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            _log.LogWarning("IdP backchannel auth: app {appId} is not configured as an Identity Provider", app_id);
            return AuthenticateResult.Fail("Application is not an Identity Provider");
        }

        // 1) Try client_secret_basic (Authorization: Basic ...)
        var basicResult = TryAuthenticateBasic(app);
        if (basicResult != null)
        {
            return basicResult;
        }

        // 2) Try private_key_jwt (client_assertion in form)
        if (Request.HasFormContentType)
        {
            Request.EnableBuffering();

            try
            {
                var form = await Request.ReadFormAsync(Context.RequestAborted);
                try
                {
                    // Reset request body stream for downstream handlers
                    if (Request.Body.CanSeek) Request.Body.Seek(0, SeekOrigin.Begin);
                }
                catch { }

                var clientAssertion = form[WellknownIdentityConstants.ClientAssertion].ToString();
                var clientAssertionType = form[WellknownIdentityConstants.ClientAssertionType].ToString();
                var clientIdFromForm = form[WellknownIdentityConstants.ClientId].ToString();

                if (!string.IsNullOrEmpty(clientAssertion) && clientAssertionType == WellknownIdentityConstants.ClientAssertionTypeJwtBearer)
                {
                    return await AuthenticatePrivateKeyJwtAsync(app, clientAssertion, clientIdFromForm);
                }

            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "IdP backchannel auth: error reading form for private_key_jwt");
                return AuthenticateResult.Fail("Invalid client_assertion");
            }
        }

        _log.LogDebug("IdP backchannel auth: no supported client authentication method present");
        return AuthenticateResult.NoResult();
    }

    private AuthenticateResult? TryAuthenticateBasic(ApplicationOption app)
    {
        string auth = Request.Headers.Authorization.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrEmpty(auth) ||
            !auth.StartsWith($"{WellknownIdentityConstants.Basic} ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var token = auth.Substring($"{WellknownIdentityConstants.Basic} ".Length).Trim();
            var credBytes = Convert.FromBase64String(token);
            var cred = Encoding.UTF8.GetString(credBytes);
            var parts = cred.Split(':', 2);

            if (parts.Length != 2)
            {
                _log.LogWarning("IdP backchannel auth: invalid Basic credential format");
                return AuthenticateResult.Fail("Invalid Basic auth");
            }

            var clientId = parts[0];
            var secret = parts[1];

            if (!ValidateClientSecret(app, clientId, secret))
            {
                _log.LogTrace("IdP backchannel auth: invalid client_secret for client_id {clientId}", clientId);
                return AuthenticateResult.Fail("Invalid client credentials");
            }

            _log.LogDebug("IdP backchannel auth: client_secret_basic success for client_id {clientId}", clientId);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, clientId),
                new Claim(ClaimTypes.Name, clientId)
            };

            var id = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(id);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (FormatException)
        {
            _log.LogWarning("IdP backchannel auth: invalid Base64 encoding in Basic header");
            return AuthenticateResult.Fail("Invalid Basic auth encoding");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "IdP backchannel auth: unexpected error during Basic auth validation");
            return AuthenticateResult.Fail("Invalid Basic auth");
        }
    }

    private async Task<AuthenticateResult> AuthenticatePrivateKeyJwtAsync(
    ApplicationOption app,
    string clientAssertion,
    string clientIdFromForm)
    {
        // Get client_id: form > iss from JWT
        string? clientId = !string.IsNullOrEmpty(clientIdFromForm)
            ? clientIdFromForm
            : GetClientIdFromJwt(clientAssertion);

        if (string.IsNullOrEmpty(clientId))
        {
            _log.LogWarning("IdP backchannel auth: client_id not provided and cannot be resolved from client_assertion");
            return AuthenticateResult.Fail("client_id not provided and not present in client_assertion");
        }

        if (app.OIDCClientConfiguration == null ||
            !app.OIDCClientConfiguration.TryGetValue(clientId, out var clientCfg) ||
            clientCfg == null)
        {
            _log.LogWarning("IdP backchannel auth: unknown client_id {clientId} for app {app}", clientId, app.ApplicationID);
            return AuthenticateResult.Fail("Unknown client_id");
        }

        var header = JwksProvider.TryReadProtectedHeader(clientAssertion);
        if (!string.IsNullOrEmpty(header.ParseError))
        {
            _log.LogWarning("IdP backchannel auth: client_assertion header parse error for client {clientId}: {error}", clientId, header.ParseError);
            return AuthenticateResult.Fail("invalid_client_assertion_header");
        }

        if (header.IsJwe)
        {
            _log.LogWarning("IdP backchannel auth: client_assertion for client {clientId} is encrypted (JWE); expected signed JWS", clientId);
            return AuthenticateResult.Fail("encrypted_client_assertion_not_supported");
        }

        var alg = header.Alg;
        if (string.IsNullOrWhiteSpace(alg))
        {
            _log.LogWarning("IdP backchannel auth: missing alg in client_assertion header for client {clientId}", clientId);
            return AuthenticateResult.Fail("unsupported_client_assertion_alg");
        }

        // Enforce IdP allowed algs
        var allowedAlgs = OIDCCryptoCapabilities.Idp.AllowedClientAssertionSigningAlgStrings;
        if (!allowedAlgs.Contains(alg))
        {
            _log.LogWarning("IdP backchannel auth: unsupported client_assertion alg {alg} for client {clientId}", alg, clientId);
            return AuthenticateResult.Fail("unsupported_client_assertion_alg");
        }

        // Get keys from cache
        IReadOnlyList<SecurityKey> signingKeys;
        try
        {
            signingKeys = await _clientSigningKeysCache
                .GetClientSigningKeysAsync(app, clientId, Context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "IdP backchannel auth: error resolving signing keys for client {clientId}", clientId);
            return AuthenticateResult.Fail("Unable to resolve client signing keys");
        }

        if (signingKeys == null || signingKeys.Count == 0)
        {
            _log.LogWarning("IdP backchannel auth: no signing keys resolved for client {clientId}", clientId);
            return AuthenticateResult.Fail("No client signing keys available");
        }

        // Validate client_assertion
        var handler = new JsonWebTokenHandler();

        var validationParams = new TokenValidationParameters
        {
            // client_assertion (RFC 7523):
            //  - iss = client_id
            //  - sub = client_id
            //  - aud = token endpoint URL
            ValidateIssuer = true,
            ValidIssuer = clientId,
            ValidateAudience = true,
            ValidAudience = $"{Request.Scheme}://{Request.Host.Host}{Request.PathBase}{Request.Path}",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,

            // Anti-downgrade: solo algs permitidos
            ValidAlgorithms = allowedAlgs
        };

        var result = await handler.ValidateTokenAsync(clientAssertion, validationParams);

        if (!result.IsValid)
        {
            _log.LogWarning("IdP backchannel auth: client_assertion validation failed for client {clientId}: {error}",
                clientId, result.Exception?.Message ?? "unknown_error");
            return AuthenticateResult.Fail("Invalid client_assertion");
        }

        // Validate sub == client_id - RFC 7523
        var jwt = (JsonWebToken?)result.SecurityToken;
        var sub = jwt?.Subject;
        if (!string.IsNullOrEmpty(sub) && sub != clientId)
        {
            _log.LogWarning("IdP backchannel auth: client_assertion sub ({sub}) does not match client_id ({clientId})", sub, clientId);
            return AuthenticateResult.Fail("Invalid client_assertion subject");
        }

        _log.LogDebug("IdP backchannel auth: private_key_jwt success for client_id {clientId}", clientId);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, clientId),
            new Claim(ClaimTypes.Name, clientId)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private bool ValidateClientSecret(ApplicationOption app, string clientId, string secret)
    {
        if (app.OIDCClientConfiguration == null) return false;
        if (!app.OIDCClientConfiguration.TryGetValue(clientId, out var cfg)) return false;

        if (string.IsNullOrEmpty(cfg.APISecret))
        {
            _log.LogTrace("IdP backchannel auth: client {clientId} has no APISecret configured, rejecting Basic auth", clientId);
            return false;
        }

        var left = Encoding.UTF8.GetBytes(cfg.APISecret);
        var right = Encoding.UTF8.GetBytes(secret);

        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static string? GetClientIdFromJwt(string jwt)
    {
        try
        {
            var handler = new JsonWebTokenHandler();
            var token = handler.ReadJsonWebToken(jwt);
            // iss should be client_id for client_assertion per RFC7523
            return token.Issuer;
        }
        catch
        {
            return null;
        }
    }
}