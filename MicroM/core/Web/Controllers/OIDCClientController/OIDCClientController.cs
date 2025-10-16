using MicroM.Configuration;
using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public ActionResult Jwks([FromServices] IMicroMAppConfiguration app_config, [FromServices] IOIDCClientService clientService, string app_id, CancellationToken ct)
    {
        var app = app_config.GetAppConfiguration(app_id);
        if (app == null) return NotFound("Application not found");

        var reqHeaders = Request.GetTypedHeaders();
        var resHeaders = Response.GetTypedHeaders();

        var result = clientService.HandleClientJwks(app, reqHeaders, resHeaders.Headers);
        if (result == null) return NotFound();

        if (result.is_cached) return StatusCode(StatusCodes.Status304NotModified);
        return Content(result.etag_content.Content, "application/json");
    }

    /// <summary>
    /// OIDC client Backchannel login: SPA posts here; server forwards to IdP PAR endpoint.
    /// Supports Authorization header pass-through, client_secret_post to Basic, or private_key_jwt from local cert.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{app_id}/oidc-client/login")]
    public async Task<ActionResult> SignInOidc([FromServices] IMicroMAppConfiguration app_config, [FromServices] IOIDCClientService clientService, string app_id, CancellationToken ct)
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest(new { error = "invalid_request", error_description = "Request must be application/x-www-form-urlencoded" });
        }

        var app = app_config.GetAppConfiguration(app_id);
        if (app == null) return NotFound("Application not found");

        var form = await Request.ReadFormAsync(ct);
        var (status, contentType, body) = await clientService.HandleSignInOidc(app, Request.Headers, form, ct);

        return new ContentResult
        {
            StatusCode = status,
            ContentType = contentType,
            Content = body
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

        if (HttpMethods.IsPost(Request.Method))
        {
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(ct);
                code = string.IsNullOrEmpty(code) ? form[WellknownIdentityConstants.Code].ToString() : code;
                redirectUri = string.IsNullOrEmpty(redirectUri) ? form[WellknownIdentityConstants.RedirectUri].ToString() : redirectUri;
                codeVerifier = string.IsNullOrEmpty(codeVerifier) ? form[WellknownIdentityConstants.CodeVerifier].ToString() : codeVerifier;
                stateIncoming = string.IsNullOrEmpty(stateIncoming) ? form[WellknownIdentityConstants.State].ToString() : stateIncoming;
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

        var (callback_result, error) = await clientService.HandleAuthorizationCallback(app, code, redirectUri, codeVerifier, stateIncoming, ct);

        if (error != null || callback_result == null || callback_result.Principal == null)
        {
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
    public Task<ActionResult> FrontChannelLogout(ApplicationOption app, string? state, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [Authorize(Policy = nameof(MicroMPermissionsConstants.IdPClientPolicy))]
    [HttpPost("{app_id}/oidc-client/back-logout")]
    public Task<ActionResult> BackchannelLogout(ApplicationOption app, string logoutTokenJwt, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
