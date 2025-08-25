using MicroM.Configuration;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;

namespace MicroM.Web.Controllers;

/// <summary>
/// Provides authentication endpoints for the MicroM API.
/// </summary>
[ApiController]
public class AuthenticationController(IOptions<MicroMOptions> options) : ControllerBase, IAuthenticationController
{
    private readonly MicroMOptions _options = options.Value;

    /// <summary>
    /// Returns a simple response indicating that the authentication API is available.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("auth-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    /// <summary>
    /// Checks whether the current user session is authenticated.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/auth/isloggedin")]
    public ActionResult IsLoggedIn()
    {
        return Ok();
    }


    /// <summary>
    /// Authenticates a user and issues authentication tokens.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{app_id}/auth/login")]
    public async Task<ActionResult> Login([FromServices] Services.IAuthenticationService aus, [FromServices] IAuthenticationProvider auth, [FromServices] WebAPIJsonWebTokenHandler jwt_handler, string app_id, [FromBody] UserLogin userLogin, CancellationToken ct)
    {
        try
        {
            // MMC: Add any additional Server claims. JwtToken is encrypted and those claims will be not be visible from the client.
            var claims = new Dictionary<string, object>();

            var (login_result, token_result) = await aus.HandleLogin(auth, jwt_handler, app_id, userLogin, claims, ct);
            if (login_result != null && token_result != null)
            {
                var result = await SignInAsync(HttpContext, token_result, login_result.refresh_token ?? "");

                // MMC: add common Client Claims.
                result.Add(MicroMClientClaimTypes.username, login_result.username);
                result.Add(MicroMClientClaimTypes.useremail, login_result.email ?? "");

                // Add the rest of the claims, if not exist
                foreach (var claim in login_result.client_claims)
                {
                    if (!result.ContainsKey(claim.Key))
                    {
                        result.Add(claim.Key, claim.Value);
                    }
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


    /// <summary>
    /// Signs the current user out and clears authentication cookies.
    /// </summary>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpPost("{app_id}/auth/logoff")]
    public async Task<ActionResult> Logoff([FromServices] IAuthenticationProvider auth, [FromServices] Services.IAuthenticationService aus, string app_id, CancellationToken ct)
    {
        try
        {
            string user_name = HttpContext.User.FindFirstValue(MicroMServerClaimTypes.MicroMUsername) ?? "";

            // If there is no user logged in, just return OK and ignore
            if (string.IsNullOrEmpty(user_name)) return Ok();

            await aus.HandleLogoff(auth, app_id, user_name, ct);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }



    /// <summary>
    /// Resets a user's password using a recovery code.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{app_id}/auth/recoverpassword")]
    public async Task<ActionResult> RecoverPassword([FromServices] Services.IAuthenticationService aus, [FromServices] IAuthenticationProvider auth, string app_id, [FromBody] UserRecoverPassword parms, CancellationToken ct)
    {
        try
        {
            var result = await aus.HandleRecoverPassword(auth, app_id, parms.Username, parms.Password, parms.RecoveryCode, ct);

            if (result.failed == false)
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
                Results = [new() { Status = DBStatusCodes.Error, Message = result.error_message }]
            };
            return Ok(error_result);
        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }
    }


    /// <summary>
    /// Sends a password recovery email.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{app_id}/auth/recoveryemail")]
    public async Task<ActionResult> RecoveryEmail([FromServices] Services.IAuthenticationService aus, [FromServices] IAuthenticationProvider auth, string app_id, [FromBody] UserRecoveryEmail parms, CancellationToken ct)
    {
        try
        {
            string username = parms.Username ?? "";

            var result = await aus.HandleSendRecoveryEmail(auth, app_id, username, ct);

            if (result.failed == false)
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


    /// <summary>
    /// Exchanges a refresh token for a new access token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{app_id}/auth/refresh")]
    public async Task<ActionResult> RefreshToken([FromServices] Services.IAuthenticationService aus, [FromServices] IAuthenticationProvider auth, [FromServices] WebAPIJsonWebTokenHandler jwt_handler, string app_id, [FromBody] UserRefreshTokenRequest user_refresh, CancellationToken ct)
    {
        try
        {
            var (refresh_result, token_result) = await aus.HandleRefreshToken(auth, jwt_handler, app_id, user_refresh, ct);
            if (refresh_result != null && refresh_result.RefreshToken != null && token_result != null)
            {
                var result = await SignInAsync(HttpContext, token_result, refresh_result.RefreshToken);

                return Ok(result);
            }

        }
        catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
        {
            return new EmptyResult();
        }

        return Unauthorized();
    }

    private static async Task<Dictionary<string, string>> SignInAsync(HttpContext httpc, TokenResult token_result, string refresh_token)
    {
        if (token_result.SD == null) throw new Exception("SecurityTokenDescriptor is null");
        if (token_result.Token == null) throw new Exception("token is null");
        if (token_result.SD.Expires == null) throw new Exception("Expires is null");

        var claimsIdentity = new ClaimsIdentity(token_result.SD.Claims.ToClaims(), CookieAuthenticationDefaults.AuthenticationScheme);

        await httpc.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties()
        {
            ExpiresUtc = token_result.SD.Expires
        });

        string expires_in = ((int)(token_result.SD.Expires - DateTime.UtcNow).Value.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                    { "access_token", token_result.Token }
                    , { "token_type", "Bearer" }
                    , { "expires_in", expires_in }
                    , { "refresh-token", refresh_token}
                };

    }

}
