using MicroM.Configuration;
using MicroM.Core;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public class IdentityProviderService(
    IOauthTokenService oauth_token_service,
    IPushedAuthorizationService par_service,
    IAuthorizationCodeService code_service,
    IEtagCacheService etag_cache,
    IApplicationCertificateCacheService certificate_cache,
    IJwksService jwks_service,
    IOIDCHttpClient oidcHttpClient,
    ILogger<IdentityProviderService> log
    ) : IIdentityProviderService
{
    private JsonSerializerOptions _jsonSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public EtagCacheServiceCacheCheckResult HandleWellKnown(ApplicationOption app, string request_base, RequestHeaders request_headers, IHeaderDictionary response_headers)
    {
        var key = $"{app.ApplicationID}_WK";

        var result = etag_cache.GetOrAddResponseWithCacheCheck(
            key,
            request_headers,
            response_headers,
            cache_duration_seconds: ConfigurationDefaults.EtagCacheDurationSeconds,
            () =>
            {
                var wellKnown = WellKnownProvider.CreateWellKnown(app, request_base);
                return JsonSerializer.Serialize(wellKnown, _jsonSerializationOptions);
            });

        return result;
    }

    public EtagCacheServiceCacheCheckResult? HandleJwks(ApplicationOption app, RequestHeaders request_headers, IHeaderDictionary response_headers)
    {
        return jwks_service.HandleJwks(app, request_headers, response_headers);
    }

    public async Task<ResultWithStatus<OIDCTokenResponse, ErrorResult>> HandleToken(ApplicationOption app, IFormCollection form, ClaimsPrincipal client, CancellationToken ct)
    {
        var authenticated_client = client.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(authenticated_client))
        {
            return new(null, new("invalid_client", "Client authentication required"));
        }

        return await oauth_token_service.HandleTokenRequest(app, form, authenticated_client, ct);
    }

    public ResultWithStatus<OIDCPARResponse, ErrorResult> HandlePAR(ApplicationOption app, IFormCollection form, ClaimsPrincipal client)
    {
        var clientId = client.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(clientId))
        {
            return new(null, new("invalid_client", "Client authentication required"));
        }

        return par_service.CreatePushedRequest(app, form, clientId);
    }

    public Task<ResultWithStatus<OIDCAuthorizeRecord, ErrorResult>> HandleAuthorize(ApplicationOption app, IQueryCollection query, ClaimsPrincipal user, string request_base, CancellationToken ct)
    {
        // Accept either a request_uri (PAR) or inline params.
        // Build an authoritative parameter set to use.
        var (qs, error) = AuthorizeEndpointProvider.ValidateAndOverrideWithPARAuthorizationRequest(app, par_service, query);

        if (qs == null || error != null)
        {
            return Task.FromResult<ResultWithStatus<OIDCAuthorizeRecord, ErrorResult>>(new(null, error));
        }

        // If user is not authenticated, redirect to interactive login SPA.
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            string login_url = AuthorizeEndpointProvider.BuildLoginURL(app, query, request_base);
            return Task.FromResult<ResultWithStatus<OIDCAuthorizeRecord, ErrorResult>>(new(new(null, login_url), null));
        }

        // User is authenticated -> issue authorization code and redirect to redirect_uri with code and state
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                     user.FindFirst(WellknownIdentityConstants.SubjectIdentifier)?.Value ??
                     user.Identity.Name ?? Guid.NewGuid().ToString("N");

        // Extract IdP session ID (sid) from the authenticated user's claims if present
        var sid = user.FindFirst(WellknownIdentityConstants.SessionIdentifier)?.Value
                  ?? user.FindFirst(MicroMServerClaimTypes.MicroMOidcSessionID)?.Value;

        // build authorization code record
        var record = new AuthorizationCodeRecord(
            Code: string.Empty,
            ClientId: qs.client_id,
            UserId: userId,
            RedirectUri: qs.redirect_uri!,
            Sid: sid,
            Nonce: string.IsNullOrWhiteSpace(qs.nonce) ? null : qs.nonce,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(5),
            CodeChallenge: string.IsNullOrEmpty(qs.code_challenge) ? null : qs.code_challenge,
            CodeChallengeMethod: string.IsNullOrEmpty(qs.code_challenge_method) ? null : qs.code_challenge_method
        );

        // store per-client
        var created_record = code_service.CreateAndStoreAuthorizationCode(app, qs.client_id, record);

        // build redirect URI with code and state
        string redirect_uri = AuthorizeEndpointProvider.BuildRedirectURI(qs.redirect_uri!, qs.state, created_record.Code);
        return Task.FromResult<ResultWithStatus<OIDCAuthorizeRecord, ErrorResult>>(new(new(redirect_uri, null), null));
    }

    public async Task<bool> HandleEndSession(ApplicationOption app, string issuer, string user_id, CancellationToken ct)
    {
        // Only IdP servers may initiate SLO fan-out
        if (app.IdentityProviderRoleType != nameof(IdentityProviderRole.IDPServer))
        {
            return false;
        }

        // Load signing certificate
        X509Certificate2? cert = certificate_cache.GetCertificate(app);
        if (cert == null)
        {
            log.LogWarning("EndSession: No certificate configured for IdP app {app}", app.ApplicationID);
            return false;
        }

        // Enumerate registered clients from configuration (server-side registry)
        var clients = app.OIDCClientConfiguration;
        if (clients == null || clients.Count == 0)
        {
            log.LogInformation("EndSession: No OIDC clients registered for app {app}", app.ApplicationID);
            return true;
        }

        var signingCreds = new X509SigningCredentials(cert);
        var handler = new JsonWebTokenHandler();

        // Fan-out logout_token to each client and purge IdP sessions by pairwise sub
        foreach (var kv in clients)
        {
            if (ct.IsCancellationRequested) break;

            var cfg = kv.Value;
            var clientAppId = cfg.ClientAPPID;
            var backUrl = cfg.URLBackchannelLogout;
            var pepper = cfg.OIDCSubjectPepper;

            if (string.IsNullOrWhiteSpace(backUrl))
            {
                log.LogWarning("EndSession: No backchannel logout URL for client {client} in app {app}", clientAppId, app.ApplicationID);
                continue;
            }

            if (string.IsNullOrWhiteSpace(pepper))
            {
                log.LogWarning("EndSession: Missing OIDCSubjectPepper for client {client} in app {app}", clientAppId, app.ApplicationID);
                continue;
            }

            // Derive pairwise sub for this client
            string sub = ApplicationOidcActiveSessions.GetDerivedSub(clientAppId, user_id, pepper);

            // Build logout_token JWT
            var now = DateTimeOffset.UtcNow;

            var claims = new List<Claim>
            {
                new(WellknownIdentityConstants.SubjectIdentifier, sub),
                new(WellknownIdentityConstants.Events, WellknownIdentityConstants.BackchannelLogoutEventJson, JsonClaimValueTypes.Json),
                new(WellknownIdentityConstants.JWTID, Guid.NewGuid().ToString("N"))
            };

            var desc = new SecurityTokenDescriptor
            {
                Issuer = issuer, // must match IdP discovery issuer
                Audience = clientAppId,
                Subject = new ClaimsIdentity(claims),
                NotBefore = now.UtcDateTime,
                IssuedAt = now.UtcDateTime,
                Expires = now.AddMinutes(5).UtcDateTime,
                SigningCredentials = signingCreds
            };

            string logoutToken = handler.CreateToken(desc);

            // POST to client back-channel endpoint
            try
            {
                var form = new[] { new KeyValuePair<string, string>("logout_token", logoutToken) };
                var res = await oidcHttpClient.PostFormUrlEncodedAsync(backUrl, form, ct);
                if (!res.IsSuccessStatusCode)
                {
                    log.LogWarning("EndSession: Backchannel POST failed for client {client} status {status} body: {body} error: {error}", clientAppId, res.StatusCode, res.Body, res.Error);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "EndSession: Backchannel POST exception for client {client}", clientAppId);
            }

            // Purge IdP sessions by sub for this client
            using var dbc = app.CreateDatabaseClient(log, null, null);
            try
            {
                await dbc.Connect(ct);
                await ApplicationOidcActiveSessions.DeleteSessionsBySUB(dbc, sub, ct);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "EndSession: Failed to purge sessions for client {client}", clientAppId);
            }
            finally
            {
                await dbc.Disconnect();
            }
        }

        return true;
    }

    public Task<Dictionary<string, object?>> HandleUserInfo(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }


}
