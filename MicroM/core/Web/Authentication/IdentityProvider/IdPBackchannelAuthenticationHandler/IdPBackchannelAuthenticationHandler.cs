using MicroM.Configuration;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    private readonly IApplicationCertificateCacheService _appCertCache;
    private readonly IJWKSFetchCacheService _jwksFetchCache;

    public IdPBackchannelAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IMicroMAppConfiguration appConfig,
        IApplicationCertificateCacheService appCertCache,
        IJWKSFetchCacheService jwksFetchCache
        ) : base(options, logger, encoder)
    {
        _appConfig = appConfig;
        _log = logger.CreateLogger<IdPBackchannelAuthenticationHandler>();
        _appCertCache = appCertCache;
        _jwksFetchCache = jwksFetchCache;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string app_id = Context.Request.RouteValues["app_id"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(app_id))
        {
            return AuthenticateResult.NoResult();
        }

        var app = _appConfig.GetAppConfiguration(app_id);
        if (app == null)
        {
            return AuthenticateResult.Fail("Unknown app");
        }

        string auth = Request.Headers.Authorization.FirstOrDefault() ?? "";
        if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var token = auth.Substring("Basic ".Length).Trim();
                var credBytes = Convert.FromBase64String(token);
                var cred = Encoding.UTF8.GetString(credBytes);
                var parts = cred.Split(':', 2);
                if (parts.Length == 2)
                {
                    var clientId = parts[0];
                    var secret = parts[1];

                    if (ValidateClientSecret(app, clientId, secret))
                    {
                        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, clientId) };
                        var id = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
                        var principal = new System.Security.Claims.ClaimsPrincipal(id);
                        var ticket = new AuthenticationTicket(principal, Scheme.Name);
                        return AuthenticateResult.Success(ticket);
                    }
                }
            }
            catch (FormatException)
            {
                return AuthenticateResult.Fail("Invalid Basic auth encoding");
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Basic auth validation error");
                return AuthenticateResult.Fail("Invalid Basic auth");
            }
        }

        if (Request.HasFormContentType)
        {
            Request.EnableBuffering();

            try
            {
                var form = await Request.ReadFormAsync();
                try
                {
                    if (Request.Body.CanSeek) Request.Body.Seek(0, SeekOrigin.Begin);
                }
                catch
                {
                    // ignore seek failures - buffering should normally allow seek
                }

                var clientAssertion = form[WellknownIdentityConstants.ClientAssertion].ToString();
                var clientAssertionType = form[WellknownIdentityConstants.ClientAssertionType].ToString();
                var clientIdFromForm = form[WellknownIdentityConstants.ClientId].ToString();

                if (!string.IsNullOrEmpty(clientAssertion) &&
                    clientAssertionType == WellknownIdentityConstants.ClientAssertionTypeJwtBearer)
                {
                    var clientId = !string.IsNullOrEmpty(clientIdFromForm) ? clientIdFromForm : GetClientIdFromJwt(clientAssertion);

                    if (string.IsNullOrEmpty(clientId))
                    {
                        return AuthenticateResult.Fail("client_id not provided and not present in client_assertion");
                    }

                    // Look up client configuration
                    if (app.OIDCClientConfiguration == null || !app.OIDCClientConfiguration.TryGetValue(clientId, out var clientCfg))
                    {
                        _log.LogWarning("Unknown client_id {clientId} for app {app}", clientId, app.ApplicationID);
                        return AuthenticateResult.Fail("Unknown client_id");
                    }

                    // Try local certificate first if client is hosted in the same server:
                    // When CertificateUniqueID is configured for this client, and there's an ApplicationOption for the client with a matching OIDCCertificateUniqueID,
                    // validate using the locally cached certificate (no network).
                    SecurityKey[]? signingKeys = null;

                    // Local
                    if (!string.IsNullOrEmpty(clientCfg.CertificateUniqueID))
                    {
                        var clientApp = _appConfig.GetAppConfiguration(clientId);
                        if (clientApp != null &&
                            !string.IsNullOrEmpty(clientApp.OIDCCertificateUniqueID) &&
                            string.Equals(clientApp.OIDCCertificateUniqueID, clientCfg.CertificateUniqueID, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                X509Certificate2? cert = _appCertCache.GetCertificate(clientApp);
                                if (cert != null)
                                {
                                    var x509Key = new X509SecurityKey(cert)
                                    {
                                        KeyId = clientApp.OIDCCertificateUniqueID
                                    };
                                    signingKeys = [x509Key];
                                    _log.LogDebug("Using local certificate for client_id {clientId} (kid={kid})", clientId, clientApp.OIDCCertificateUniqueID);
                                }
                                else
                                {
                                    _log.LogWarning("Local certificate not available for hosted client {clientId}", clientId);
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.LogWarning(ex, "Error loading local certificate for client_id {clientId}", clientId);
                            }
                        }
                    }

                    // Fallback: Fetch client's JWKS using shared cache (ETag/TTL aware)
                    if ((signingKeys == null || signingKeys.Length == 0))
                    {
                        if (string.IsNullOrEmpty(clientCfg.URLClientJWKS))
                        {
                            _log.LogWarning("Client JWKS URL not configured and no local certificate found for client {clientId}", clientId);
                            return AuthenticateResult.Fail("Client JWKS URL not configured");
                        }

                        try
                        {
                            var jwksResult = await _jwksFetchCache.GetAsync(clientCfg.URLClientJWKS, Context.RequestAborted);
                            signingKeys = jwksResult.Keys.Values.ToArray();
                        }
                        catch (Exception ex)
                        {
                            _log.LogWarning(ex, "Failed to fetch client jwks for {app_id} {clientId}", app.ApplicationID, clientId);
                            return AuthenticateResult.Fail("Unable to retrieve client JWKS");
                        }
                    }

                    // Validate client_assertion JWT using the obtained keys
                    var handler = new JsonWebTokenHandler();
                    var validationParams = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = clientId,
                        ValidateAudience = true,
                        ValidAudience = $"{Request.Scheme}://{Request.Host.Value}{Request.Path}",
                        IssuerSigningKeys = signingKeys,
                        RequireSignedTokens = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };

                    var result = await handler.ValidateTokenAsync(clientAssertion, validationParams);

                    // Rotation fallback: force a JWKS refresh once and retry if validation fails
                    if (!result.IsValid && !string.IsNullOrEmpty(clientCfg.URLClientJWKS))
                    {
                        try
                        {
                            var refreshed = await _jwksFetchCache.GetAsync(clientCfg.URLClientJWKS, Context.RequestAborted, forceRefresh: true);
                            var refreshedKeys = refreshed.Keys.Values.ToArray();
                            if (refreshedKeys.Length > 0)
                            {
                                validationParams.IssuerSigningKeys = refreshedKeys;
                                result = await handler.ValidateTokenAsync(clientAssertion, validationParams);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.LogDebug(ex, "Forced JWKS refresh failed for client {clientId}", clientId);
                        }
                    }

                    if (!result.IsValid)
                    {
                        _log.LogWarning("client_assertion validation failed for {clientId}: {error}", clientId, result.Exception?.Message);
                        return AuthenticateResult.Fail("Invalid client_assertion");
                    }

                    // Success
                    var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, clientId) };
                    var id = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
                    var principal = new System.Security.Claims.ClaimsPrincipal(id);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);
                    return AuthenticateResult.Success(ticket);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "private_key_jwt validation error");
                return AuthenticateResult.Fail("Invalid client_assertion");
            }
        }

        return AuthenticateResult.NoResult();
    }

    private static bool ValidateClientSecret(ApplicationOption app, string clientId, string secret)
    {
        if (app.OIDCClientConfiguration == null) return false;
        if (!app.OIDCClientConfiguration.TryGetValue(clientId, out var cfg)) return false;

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