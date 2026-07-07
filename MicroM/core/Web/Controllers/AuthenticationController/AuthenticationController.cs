using MicroM.Configuration;
using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Authentication.SSO;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace MicroM.Web.Controllers;

[ApiController]
public class AuthenticationController(IOptions<MicroMOptions> options) : ControllerBase, IAuthenticationController
{
    private readonly MicroMOptions _options = options.Value;

    [AllowAnonymous]
    [HttpGet("auth-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/auth/isloggedin")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthIsLoggedInPolicy)]
    public ActionResult IsLoggedIn()
    {
        return Ok();
    }


    [AllowAnonymous]
    [HttpPost("{app_id}/auth/login")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthLoginPolicy)]
    public async Task<ActionResult> Login(
        [FromServices] Services.IAuthenticationService aus,
        [FromServices] IAuthenticationProvider auth,
        [FromServices] WebAPIJsonWebTokenHandler jwt_handler,
        [FromServices] IMicroMAppConfiguration app_config,
        string app_id,
        [FromBody] UserLogin userLogin,
        CancellationToken ct)
    {
        try
        {
            var app = app_config.GetAppConfiguration(app_id);
            if (app == null) return NotFound("Application not found");

            if (app.IdentityProviderRoleType == nameof(IdentityProviderRole.IDPClient))
            {
                return BadRequest("This application is configured as an Identity Provider Client, use the OIDC endpoints");
            }

            // MMC: Add any additional Server claims. JwtToken is encrypted and those claims will be not be visible from the client.
            var claims = new Dictionary<string, object>();

            var (login_result, token_result) = await aus.HandleLogin(auth, jwt_handler, app_id, userLogin, claims, ct);
            if (login_result != null && token_result != null)
            {
                var result = await aus.SignInAsync(HttpContext, token_result, login_result.refresh_token ?? "");

                // MMC: add common Client Claims.
                result.Add(MicroMClientClaimTypes.username, login_result.username);
                result.Add(MicroMClientClaimTypes.useremail, login_result.email ?? "");

                // Add the rest of the claims, if not exist
                foreach (var claim in login_result.client_claims)
                {
                    result.TryAdd(claim.Key, claim.Value);
                }

                return Ok(result);
            }

            if (login_result?.requires_two_factor == true)
            {
                return Ok(login_result);
            }

        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }

        return Unauthorized();
    }


    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/auth/totp/setup")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthLoginPolicy)]
    public async Task<ActionResult> StartTotpSetup(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] ITotpService totpService,
        string app_id,
        CancellationToken ct)
    {
        try
        {
            string user_name = HttpContext.User.FindFirstValue(MicroMServerClaimTypes.MicroMUsername) ?? "";
            var result = await totpService.HandleStartTotpSetup(auth, app_id, user_name, HttpContext.User.Claims.ToClaimsDictionary(), ct);

            return MapTotpServiceResult(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }


    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/auth/totp/confirm")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthLoginPolicy)]
    public async Task<ActionResult> ConfirmTotpSetup(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] ITotpService totpService,
        string app_id,
        [FromBody] TotpConfirmRequest request,
        CancellationToken ct)
    {
        try
        {
            string user_name = HttpContext.User.FindFirstValue(MicroMServerClaimTypes.MicroMUsername) ?? "";
            var result = await totpService.HandleConfirmTotpSetup(auth, app_id, user_name, request, HttpContext.User.Claims.ToClaimsDictionary(), ct);

            return MapTotpServiceResult(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }


    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/auth/totp/disable")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthLogoffPolicy)]
    public async Task<ActionResult> DisableTotp(
        [FromServices] IAuthenticationProvider auth,
        [FromServices] ITotpService totpService,
        string app_id,
        CancellationToken ct)
    {
        try
        {
            string user_name = HttpContext.User.FindFirstValue(MicroMServerClaimTypes.MicroMUsername) ?? "";
            var result = await totpService.HandleDisableTotp(auth, app_id, user_name, HttpContext.User.Claims.ToClaimsDictionary(), ct);

            if (result.Status == TotpServiceResultStatus.Success)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            return MapTotpServiceResult(result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }


    [AllowAnonymous]
    [HttpPost("{app_id}/auth/login-2fa")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthLoginPolicy)]
    public async Task<ActionResult> VerifyTwoFactor(
        [FromServices] Services.IAuthenticationService aus,
        [FromServices] IAuthenticationProvider auth,
        [FromServices] WebAPIJsonWebTokenHandler jwt_handler,
        string app_id,
        [FromBody] TwoFactorLoginRequest request,
        CancellationToken ct)
    {
        try
        {
            var claims = new Dictionary<string, object>();
            var (login_result, token_result) = await aus.HandleTwoFactorLogin(auth, jwt_handler, app_id, request, claims, ct);
            if (login_result != null && token_result != null)
            {
                var result = await aus.SignInAsync(HttpContext, token_result, login_result.refresh_token ?? "");

                result.Add(MicroMClientClaimTypes.username, login_result.username);
                result.Add(MicroMClientClaimTypes.useremail, login_result.email ?? "");

                foreach (var claim in login_result.client_claims)
                {
                    result.TryAdd(claim.Key, claim.Value);
                }

                return Ok(result);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }

        return Unauthorized();
    }

    private ActionResult MapTotpServiceResult(TotpServiceResult result)
    {
        return result.Status switch
        {
            TotpServiceResultStatus.Success when result.SetupResponse != null => Ok(result.SetupResponse),
            TotpServiceResultStatus.Success => Ok(),
            TotpServiceResultStatus.AppNotFound => NotFound("Application not found"),
            TotpServiceResultStatus.UnsupportedAuthenticator => BadRequest("TOTP is only supported for MicroM authentication."),
            TotpServiceResultStatus.InvalidUser => Unauthorized(),
            TotpServiceResultStatus.SetupNotStarted => BadRequest("TOTP setup has not been started."),
            TotpServiceResultStatus.InvalidCode => Unauthorized(),
            TotpServiceResultStatus.DatabaseFailure => BadRequest(result.DatabaseResult),
            _ => BadRequest()
        };
    }


    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/auth/logoff")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthLogoffPolicy)]
    public async Task<ActionResult> Logoff([FromServices] IAuthenticationProvider auth, [FromServices] Services.IAuthenticationService aus, string app_id, CancellationToken ct)
    {
        try
        {
            string user_name = HttpContext.User.FindFirstValue(MicroMServerClaimTypes.MicroMUsername) ?? "";
            string user_id = HttpContext.User.FindFirstValue(MicroMServerClaimTypes.MicroMUser_id) ?? "";

            // If there is no user logged in, just return OK and ignore
            if (string.IsNullOrEmpty(user_name)) return Ok();

            await aus.HandleLogoff(auth, app_id, user_name, user_id, ct);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }



    [AllowAnonymous]
    [HttpPost("{app_id}/auth/recoverpassword")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthRecoveryPolicy)]
    public async Task<ActionResult> RecoverPassword([FromServices] Services.IAuthenticationService aus, [FromServices] IAuthenticationProvider auth, string app_id, [FromBody] UserRecoverPassword parms, CancellationToken ct)
    {
        try
        {
            var (failed, status) = await aus.HandleRecoverPassword(auth, app_id, parms.Username, parms.Password, parms.RecoveryCode, ct);

            if (failed == false)
            {
                DBStatusResult db_result = new()
                {
                    Failed = false,
                    Results = [new() { Status = DBStatusCodes.OK, Message = "OK" }]
                };
                return Ok(db_result);
            }

            DBStatusResult error_result = new()
            {
                Failed = true,
                Results = [new() { Status = DBStatusCodes.Error, Message = status }]
            };
            return Ok(error_result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }


    [AllowAnonymous]
    [HttpPost("{app_id}/auth/recoveryemail")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingAuthRecoveryPolicy)]
    public async Task<ActionResult> RecoveryEmail([FromServices] Services.IAuthenticationService aus, [FromServices] IAuthenticationProvider auth, string app_id, [FromBody] UserRecoveryEmail parms, CancellationToken ct)
    {
        try
        {
            string username = parms.Username ?? "";

            var (failed, status) = await aus.HandleSendRecoveryEmail(auth, app_id, username, ct);

            if (failed == false)
            {
                DBStatusResult db_result = new()
                {
                    Failed = false,
                    Results = [new() { Status = DBStatusCodes.OK, Message = "OK" }]
                };
                return Ok(db_result);
            }

            DBStatusResult error_result = new()
            {
                Failed = true,
                Results = [new() { Status = DBStatusCodes.Error, Message = "Is not possible to send the recovery email, review your information and try again later" }]
            };
            return Ok(error_result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }


    [AllowAnonymous]
    [HttpPost("{app_id}/auth/refresh")]
    [EnableRateLimiting(MicroMServicesConstants.RateLimitingRefreshPolicy)]
    public async Task<ActionResult> RefreshToken(
        [FromServices] Services.IAuthenticationService aus,
        [FromServices] IAuthenticationProvider auth,
        [FromServices] WebAPIJsonWebTokenHandler jwt_handler,
        [FromServices] IOIDCClientService oidc_client,
        string app_id,
        [FromBody] UserRefreshTokenRequest user_refresh,
        CancellationToken ct)
    {
        try
        {
            var (refresh_result, token_result) = await aus.HandleRefreshToken(auth, jwt_handler, oidc_client, app_id, user_refresh, ct);
            if (refresh_result != null && refresh_result.RefreshToken != null && token_result != null)
            {
                var result = await aus.SignInAsync(HttpContext, token_result, refresh_result.RefreshToken);

                return Ok(result);
            }

        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }

        return Unauthorized();
    }

}
