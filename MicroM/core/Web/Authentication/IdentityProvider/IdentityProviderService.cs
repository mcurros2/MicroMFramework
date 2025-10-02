using MicroM.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace MicroM.Web.Authentication.SSO;

public class IdentityProviderService(
    IOauthTokenService oauth_token_service,
    IPushedAuthorizationService par_service,
    IAuthorizationCodeService code_service,
    IEtagCacheService etag_cache,
    IJwksService jwks_service
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

    public (OIDCTokenResponse? response, object? error) HandleToken(ApplicationOption app, IFormCollection form, ClaimsPrincipal client)
    {
        var authenticated_client = client.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(authenticated_client))
        {
            return new(null, new { error = "invalid_client", error_description = "Client authentication required" });
        }

        return oauth_token_service.HandleTokenRequest(app, form, authenticated_client);
    }

    public (OIDCPARResponse? response, object? error) HandlePAR(ApplicationOption app, IFormCollection form, ClaimsPrincipal client)
    {
        var clientId = client.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(clientId))
        {
            return (null, new { error = "invalid_client", error_description = "Client authentication required" });
        }

        return par_service.CreatePushedRequest(app, form, clientId);
    }

    public Task<(string? redirectUrl, string? loginUrl, object? error)> HandleAuthorize(ApplicationOption app, IQueryCollection query, ClaimsPrincipal user, string request_base, CancellationToken ct)
    {
        // Accept either a request_uri (PAR) or inline params.
        // Build an authoritative parameter set to use.
        var (qs, error) = AuthorizeEndpointProvider.ValidateAndOverrideWithPARAuthorizationRequest(app, par_service, query);

        if (qs == null || error != null)
        {
            return Task.FromResult<(string? redirectUrl, string? loginUrl, object? error)>((null, null, error));
        }

        // If user is not authenticated, redirect to interactive login SPA.
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            string login_url = AuthorizeEndpointProvider.BuildLoginURL(app, query, request_base);
            return Task.FromResult<(string? redirectUrl, string? loginUrl, object? error)>((null, login_url, null));
        }

        // User is authenticated -> issue authorization code and redirect to redirect_uri with code and state
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                     user.FindFirst("sub")?.Value ??
                     user.Identity.Name ?? Guid.NewGuid().ToString("N");

        // Extract IdP session ID (sid) from the authenticated user's claims if present
        var sid = user.FindFirst("sid")?.Value
                  ?? user.FindFirst(MicroMServerClaimTypes.MicroMOidcSessionID)?.Value;

        // build authorization code record
        var record = new AuthorizationCodeRecord(
            Code: string.Empty,
            ClientId: qs.client_id,
            UserId: userId,
            RedirectUri: qs.redirect_uri!,
            Sid: sid,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(5),
            CodeChallenge: string.IsNullOrEmpty(qs.code_challenge) ? null : qs.code_challenge,
            CodeChallengeMethod: string.IsNullOrEmpty(qs.code_challenge_method) ? null : qs.code_challenge_method
        );

        // store per-client
        var created_record = code_service.CreateAndStoreAuthorizationCode(app, qs.client_id, record);

        // build redirect URI with code and state
        string redirect_uri = AuthorizeEndpointProvider.BuildRedirectURI(qs.redirect_uri!, qs.state, created_record.Code);
        return Task.FromResult<(string? redirectUrl, string? loginUrl, object? error)>((redirect_uri, null, null));
    }

    public Task<bool> HandleEndSession(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, object?>> HandleUserInfo(ApplicationOption app_config, string app_id, string user_id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }


}
