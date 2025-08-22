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
/// Provides user authentication, token issuance, and session management endpoints.
/// </summary>
[ApiController]
/// <summary>
/// Provides user authentication, token issuance, and session management endpoints.
/// </summary>
public class AuthenticationController(IOptions<MicroMOptions> options) : ControllerBase, IAuthenticationController
{
    private readonly MicroMOptions _options = options.Value;

    /// <summary>
    /// Checks whether the authentication API is responsive.
    /// </summary>
    /// <returns>Always returns "OK" when the service is running.</returns>
    [AllowAnonymous]
    [HttpGet("auth-api-status")]
    public string GetStatus()
    {
        return "OK";
    }

    /// <summary>
    /// Determines whether the current user session is authenticated.
    /// </summary>
    /// <returns><see cref="OkResult"/> when the user is authenticated; otherwise the framework returns <see cref="UnauthorizedResult"/>.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller is not authorized to access the endpoint.</exception>
    [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
    [HttpGet("{app_id}/auth/isloggedin")]
    public ActionResult IsLoggedIn()
    {
        return Ok();
    }


    /// <summary>
    /// Validates credentials and returns a JSON Web Token and refresh token.
    /// </summary>
    /// <param name="aus">Authentication service used to validate credentials and issue tokens.</param>
    /// <param name="auth">Provider that accesses user information for validation.</param>
    /// <param name="jwt_handler">Handler responsible for generating JWTs.</param>
    /// <param name="app_id">Identifier of the application requesting authentication.</param>
    /// <param name="userLogin">User credentials payload.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>
    /// A dictionary containing access and refresh tokens when authentication succeeds;
    /// <see cref="UnauthorizedResult"/> when credentials are invalid; or <see cref="EmptyResult"/> if the request is cancelled.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
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
    /// Terminates the current user's session and clears authentication cookies.
    /// </summary>
    /// <param name="auth">Provider used to revoke the user's session.</param>
    /// <param name="aus">Authentication service that performs logoff logic.</param>
    /// <param name="app_id">Identifier of the application owning the session.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns><see cref="OkResult"/> when logoff succeeds or no user is logged in; otherwise <see cref="EmptyResult"/> if the request is cancelled.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller is not authorized to access the endpoint.</exception>
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
    /// <param name="aus">Authentication service that handles password recovery.</param>
    /// <param name="auth">Provider used to manage user accounts.</param>
    /// <param name="app_id">Identifier of the target application.</param>
    /// <param name="parms">Parameters containing the username, new password, and recovery code.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>A <see cref="DBStatusResult"/> indicating success or failure, or <see cref="EmptyResult"/> if the request is cancelled.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
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
    /// Sends a password recovery email to the specified user.
    /// </summary>
    /// <param name="aus">Authentication service that sends the recovery email.</param>
    /// <param name="auth">Provider used to locate the user account.</param>
    /// <param name="app_id">Identifier of the application.</param>
    /// <param name="parms">Parameters containing the username to recover.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>A <see cref="DBStatusResult"/> indicating whether the email was sent, or <see cref="EmptyResult"/> if the request is cancelled.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
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
    /// Issues a new access token pair based on a valid refresh token.
    /// </summary>
    /// <param name="aus">Authentication service that validates and refreshes tokens.</param>
    /// <param name="auth">Provider used to verify the refresh token.</param>
    /// <param name="jwt_handler">Handler responsible for generating new JWTs.</param>
    /// <param name="app_id">Identifier of the application.</param>
    /// <param name="user_refresh">Refresh token request payload.</param>
    /// <param name="ct">Token to observe cancellation requests.</param>
    /// <returns>
    /// A dictionary containing the new access and refresh tokens when the refresh is successful;
    /// <see cref="UnauthorizedResult"/> when the refresh token is invalid; or <see cref="EmptyResult"/> if the request is cancelled.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
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
