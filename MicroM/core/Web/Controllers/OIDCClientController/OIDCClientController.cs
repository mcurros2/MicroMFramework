using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Security.Claims;
using IAuthenticationService = MicroM.Web.Services.IAuthenticationService;

namespace MicroM.Web.Controllers;

[ApiController]
public class OIDCClientController : ControllerBase, IOIDCClientController
{
    /// <summary>
    /// Expose this client's JWKS (public keys) for OIDC (e.g., private_key_jwt).
    /// Uses the application's configured certificate.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{app_id}/oidc-client/jwks")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingOidcMetadataPolicy)]
    public ActionResult Jwks([FromServices] IMicroMAppConfiguration app_config, [FromServices] IOIDCClientService clientService, string app_id, CancellationToken ct)
    {
        var app = app_config.GetAppConfiguration(app_id);
        if (app == null) return NotFound("Application not found");

        var reqHeaders = Request.GetTypedHeaders();
        var resHeaders = Response.GetTypedHeaders();

        var result = clientService.HandleClientJwks(app, reqHeaders, resHeaders.Headers);
        if (result == null) return NotFound();

        if (result.not_modified) return StatusCode(StatusCodes.Status304NotModified);
        return Content(result.etag_content.Content, MediaTypeNames.Application.Json);
    }

    /// <summary>
    /// OIDC client Backchannel login: SPA posts here; server forwards to IdP PAR endpoint.
    /// Supports Authorization header pass-through, client_secret_post to Basic, or private_key_jwt from local cert.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{app_id}/oidc-client/par")]
    public async Task<ActionResult> ClientPAR(IMicroMAppConfiguration app_config, [FromServices] IOIDCClientService clientService, string app_id, CancellationToken ct)
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest(new { error = "invalid_request", error_description = "Request must be application/x-www-form-urlencoded" });
        }

        var app = app_config.GetAppConfiguration(app_id);
        if (app == null) return NotFound("Application not found");

        var form = await Request.ReadFormAsync(ct);
        var result = await clientService.HandleOidcClientPAR(app, Request.Headers, form, ct);

        return new ContentResult
        {
            StatusCode = result.StatusCode,
            ContentType = result.ContentType,
            Content = result.Body
        };
    }

    /// <summary>
    /// OIDC authorization code callback endpoint (client side).
    /// Accepts either GET query parameters or application/x-www-form-urlencoded POST body:
    /// Required: code, redirect_uri, code_verifier
    /// Returns principal claims upon success.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{app_id}/oidc-client/auth-callback")]
    [HttpPost("{app_id}/oidc-client/auth-callback")]
    public async Task<ActionResult> AuthorizeCallback(
        [FromServices] IMicroMAppConfiguration app_config,
        [FromServices] IOIDCClientService clientService,
        [FromServices] IDeviceIdService deviceid_service,
        [FromServices] IAuthenticationService auth_service,
        [FromServices] WebAPIJsonWebTokenHandler jwt_handler,
        [FromServices] IAuthenticationProvider auth,
        [FromServices] ILogger<OIDCClientController> log,
        string app_id,
        CancellationToken ct)
    {
        var app = app_config.GetAppConfiguration(app_id);
        if (app == null) return NotFound("Application not found");

        var authenticator = auth.GetAuthenticator(app);
        if (authenticator == null)
        {
            log.LogWarning("No authenticator configured for app {app}", app.ApplicationID);
            return BadRequest(new { error = "server_error", error_description = "No authenticator configured for this app" });
        }

        // Extract parameters from query or form (POST)
        string? code = Request.Query[WellknownIdentityConstants.Code];
        string? redirectUri = Request.Query[WellknownIdentityConstants.RedirectUri];
        string? codeVerifier = Request.Query[WellknownIdentityConstants.CodeVerifier];
        string? stateIncoming = Request.Query[WellknownIdentityConstants.State];
        string? authorizationResponseIssuer = Request.Query[WellknownIdentityConstants.IssuerClaim]; // 'iss' mix-up mitigation

        if (HttpMethods.IsPost(Request.Method))
        {
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(ct);
                code = form[WellknownIdentityConstants.Code].ToString();
                redirectUri = form[WellknownIdentityConstants.RedirectUri].ToString();
                codeVerifier = form[WellknownIdentityConstants.CodeVerifier].ToString();
                stateIncoming = form[WellknownIdentityConstants.State].ToString();
                // Ensure 'iss' is captured for POST callbacks
                authorizationResponseIssuer = form[WellknownIdentityConstants.IssuerClaim].ToString();
            }
            else
            {
                return BadRequest(new { error = "invalid_request", error_description = "POST must be form-urlencoded" });
            }
        }

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(redirectUri) ||
            string.IsNullOrWhiteSpace(codeVerifier) ||
            string.IsNullOrWhiteSpace(stateIncoming)
            )
        {
            return BadRequest(new { error = "invalid_request", error_description = "code, redirect_uri, code_verifier and state are required" });
        }

        var (callback_result, error) =
            await clientService.HandleAuthorizationCallback(app, code, redirectUri, codeVerifier, stateIncoming, authorizationResponseIssuer, ct);

        if (error != null || callback_result == null || callback_result.Principal == null)
        {
            // Map specific issuer mismatch to clearer error
            if (error == "invalid_authorization_response_iss")
            {
                return BadRequest(new { error = "access_denied", error_description = "authorization response issuer mismatch" });
            }
            return BadRequest(new { error = "access_denied", error_description = error ?? "Callback failed" });
        }

        // Build MicroM server-claims and issue local session using centralized SignInAsync
        var (device_id, _, _) = deviceid_service.GetDeviceID(local_device_id: callback_result.DeviceId ?? "");
        string username = callback_result.Principal.FindFirstValue(WellknownIdentityConstants.PreferredUsername)
                          ?? callback_result.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? callback_result.Principal.FindFirstValue(WellknownIdentityConstants.SubjectIdentifier)
                          ?? string.Empty;
        if (string.IsNullOrEmpty(username))
        {
            return BadRequest(new { error = "invalid_userinfo", error_description = "Missing subject/username in id_token" });
        }

        string? sub = callback_result.Principal.FindFirstValue(WellknownIdentityConstants.SubjectIdentifier);
        string? sid = callback_result.Principal.FindFirstValue(WellknownIdentityConstants.SessionIdentifier);
        string? email = callback_result.Principal.FindFirstValue(ClaimTypes.Email) ?? callback_result.Principal.FindFirst("email")?.Value;


        // Call authenticator to JIT-provision and issue per-device refresh
        var claimsDict = callback_result.Principal.Claims
            .GroupBy(c => c.Type, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last().Value, StringComparer.Ordinal);

        var externalIdentity = new ExternalIdentity(
            Provider: WellknownIdentityConstants.Oidc,
            Subject: sub,
            Username: username,
            Email: email,
            SessionId: sid,
            IdpRefreshToken: callback_result.IdpRefreshToken,
            IdpRefreshExpirationUtc: callback_result.IdpRefreshExpirationUtc,
            Claims: claimsDict
        );

        var extResult = await authenticator.HandleExternalSignIn(app, externalIdentity, device_id, ct);


        var token_result = jwt_handler.GenerateJwtTokenWEBApi(extResult.ServerClaims, app);
        if (token_result?.Token == null || token_result.SD?.Expires == null)
        {
            return BadRequest(new { error = "server_error", error_description = "Failed to build local session token" });
        }

        var response = await auth_service.SignInAsync(HttpContext, token_result, refresh_token: "");

        // Add minimal client-facing claims (consistent with /auth/login behavior)
        response[MicroMClientClaimTypes.username] = username;
        response[MicroMClientClaimTypes.useremail] = callback_result.Principal.FindFirstValue(ClaimTypes.Email) ?? "";

        // Add the rest of the claims, if not exist
        foreach (var claim in extResult.ClientClaims)
        {
            response.TryAdd(claim.Key, claim.Value);
        }

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("{app_id}/oidc-client/front-logout")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingFrontchannelLogoutPolicy)]
    public async Task<ActionResult> FrontChannelLogout(
        [FromServices] IMicroMAppConfiguration app_config,
        [FromServices] IOIDCClientService oidc_client,
        [FromServices] ILogger<OIDCClientController> log,
        [FromQuery] string? state,
        string app_id,
        CancellationToken ct)
    {

        var appOpt = app_config.GetAppConfiguration(app_id);
        if (appOpt == null)
        {
            return NotFound("Application not found");
        }

        var (ok, err) = await oidc_client.HandleFrontChannelLogout(appOpt, state, ct);
        if (err != null || !ok)
        {
            log.LogWarning("Front-channel logout processing failed for app {app}: {err}", app_id, err ?? "unknown");
            return BadRequest(new { error = "logout_failed", error_description = err ?? "unknown" });
        }

        return Ok(true);
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpPost("{app_id}/oidc-client/back-logout")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingBackchannelLogoutPolicy)]
    [IgnoreAntiforgeryToken] // Backchannel is machine-to-machine; disable antiforgery enforcement
    public async Task<ActionResult> BackchannelLogout(
        [FromServices] IMicroMAppConfiguration app_config,
        [FromServices] IOIDCClientService oidc_client,
        [FromServices] ILogger<OIDCClientController> log,
        string app_id,
        CancellationToken ct)
    {
        var appOpt = app_config.GetAppConfiguration(app_id);
        if (appOpt == null)
        {
            return NotFound("Application not found");
        }

        // Must be form-urlencoded with logout_token per spec
        if (!Request.HasFormContentType)
        {
            return BadRequest(new { error = "invalid_request", error_description = "Request must be application/x-www-form-urlencoded" });
        }

        var form = await Request.ReadFormAsync(ct);
        var logoutToken = form["logout_token"].ToString();

        if (string.IsNullOrWhiteSpace(logoutToken))
        {
            return BadRequest(new { error = "invalid_request", error_description = "logout_token is required" });
        }

        var result = await oidc_client.HandleBackchannelLogout(appOpt, logoutToken, ct);

        // Return 200 for success and replay/idempotent outcomes; 400 for validation failures.
        switch (result.Status)
        {
            case OIDCLogoutProcessingStatus.Success:
            case OIDCLogoutProcessingStatus.Replay:
            case OIDCLogoutProcessingStatus.AlreadyProcessed:
                return Ok(true);

            case OIDCLogoutProcessingStatus.InvalidSignature:
            case OIDCLogoutProcessingStatus.InvalidAudience:
            case OIDCLogoutProcessingStatus.InvalidIssuer:
            case OIDCLogoutProcessingStatus.MissingEvent:
            case OIDCLogoutProcessingStatus.MissingSidOrSub:
            case OIDCLogoutProcessingStatus.Expired:
            case OIDCLogoutProcessingStatus.UnknownSid:
            case OIDCLogoutProcessingStatus.SessionStoreError:
            default:
                log.LogWarning("Backchannel logout failed for app {app}: {status} {err}", app_id, result.Status, result.Error ?? "");
                return BadRequest(new { error = "logout_failed", error_description = result.Error ?? result.Status.ToString() });
        }
    }
}
