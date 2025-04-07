using MicroM.Configuration;
using MicroM.Data;
using MicroM.Web.Authentication;
using MicroM.Web.Services;
using MicroM.Web.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;
using static MicroM.Extensions.SecurityExtensions;

namespace MicroM.Web.Controllers
{
    [ApiController]
    public class MicroMController(IOptions<MicroMOptions> options) : ControllerBase
    {
        private readonly MicroMOptions _options = options.Value;

        [AllowAnonymous]
        [HttpGet("api-status")]
        public static string GetStatus()
        {
            return "OK";
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


        [AllowAnonymous]
        [HttpPost("{app_id}/refresh")]
        public async Task<ActionResult> RefreshToken([FromServices] IMicroMWebAPI api, [FromServices] IAuthenticationProvider auth, [FromServices] WebAPIJsonWebTokenHandler jwt_handler, string app_id, [FromBody] UserRefreshTokenRequest user_refresh, CancellationToken ct)
        {
            var (refresh_result, token_result) = await api.HandleRefreshToken(auth, jwt_handler, app_id, user_refresh, ct);
            if (refresh_result != null && refresh_result.RefreshToken != null && token_result != null)
            {
                var result = await SignInAsync(HttpContext, token_result, refresh_result.RefreshToken);

                return Ok(result);
            }

            return Unauthorized();
        }


        [AllowAnonymous]
        [HttpPost("{app_id}/login")]
        public async Task<ActionResult> Login([FromServices] IMicroMWebAPI api, [FromServices] IAuthenticationProvider auth, [FromServices] WebAPIJsonWebTokenHandler jwt_handler, string app_id, [FromBody] UserLogin userLogin, CancellationToken ct)
        {
            // MMC: Add any additional Server claims. JwtToken is encrypted and those claims will be not be visible from the client.
            var claims = new Dictionary<string, object>();

            var (login_result, token_result) = await api.HandleLogin(auth, jwt_handler, app_id, userLogin, claims, ct);
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

            return Unauthorized();
        }

        [AllowAnonymous]
        [HttpPost("{app_id}/recoveryemail")]
        public async Task<ActionResult> RecoveryEmail([FromServices] IMicroMWebAPI api, [FromServices] IAuthenticationProvider auth, string app_id, [FromBody] UserRecoveryEmail parms, CancellationToken ct)
        {

            string username = parms.Username ?? "";

            var result = await api.HandleSendRecoveryEmail(auth, app_id, username, ct);

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

        [AllowAnonymous]
        [HttpPost("{app_id}/recoverpassword")]
        public async Task<ActionResult> RecoverPassword([FromServices] IMicroMWebAPI api, [FromServices] IAuthenticationProvider auth, string app_id, [FromBody] UserRecoverPassword parms, CancellationToken ct)
        {
            var result = await api.HandleRecoverPassword(auth, app_id, parms.Username, parms.Password, parms.RecoveryCode, ct);

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

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/logoff")]
        public async Task<ActionResult> Logoff([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, CancellationToken ct)
        {
            string user_name = HttpContext.User.FindFirstValue(MicroMServerClaimTypes.MicroMUsername) ?? "";
            
            // If there is no user logged in, just return OK and ignore
            if (string.IsNullOrEmpty(user_name)) return Ok();

            await api.HandleLogoff(auth, app_id, user_name, ct);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpGet("{app_id}/isloggedin")]
        public ActionResult IsLoggedIn()
        {
            return Ok();
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/tmpupload")]
        public async Task<ObjectResult> Upload([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, [FromQuery] string fileprocess_id, [FromQuery] string file_name, [FromQuery] int? maxSize, [FromQuery] int? quality, CancellationToken ct)
        {
            //HttpContext.Connection.RemoteIpAddress.ToString();
            var serverClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, serverClaims, auth, ct);

            var result = await api.HandleUpload(app_id, fileprocess_id, file_name, Request.Body, maxSize, quality, ec, ct);

            return Ok(result);
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpGet("{app_id}/serve/{fileguid}")]
        public async Task<IActionResult> Serve([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string fileguid, CancellationToken ct)
        {
            var serverClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, serverClaims, auth, ct);

            var result = await api.HandleServe(app_id, fileguid, ec, ct);

            if (result == null) return NotFound();

            return File(result.FileStream, result.ContentType);
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpGet("{app_id}/thumbnail/{fileguid}/{maxSize?}/{quality?}")]
        public async Task<IActionResult> ServeThumbnail([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string fileguid, int? maxSize, int? quality, CancellationToken ct)
        {
            var serverClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, serverClaims, auth, ct);

            var result = await api.HandleServeThumbnail(app_id, fileguid, maxSize, quality, ec, ct);

            if (result == null) return NotFound();

            return File(result.FileStream, result.ContentType);
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpGet("{app_id}/{entityName}/definition")]
        public ObjectResult GetDefinition([FromServices] IMicroMWebAPI api, string app_id, string entityName)
        {
            var result = api.HandleGetEntityDefinition(app_id, entityName);
            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/get")]
        public async Task<ObjectResult> Get([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();

            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleGetEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/insert")]
        public async Task<ObjectResult> Insert(
            [FromServices] IAuthenticationProvider auth,
            [FromServices] IMicroMWebAPI api,
            [FromBody] DataWebAPIRequest parms,
            string app_id, string entityName,
            CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleInsertEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }


        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/update")]
        public async Task<ObjectResult> Update([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleUpdateEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/delete")]
        public async Task<ObjectResult> Delete([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleDeleteEntity(auth, app_id, entityName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/lookup/{lookupName?}")]
        public async Task<ObjectResult> Lookup([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string? lookupName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleLookupEntity(auth, app_id, entityName, parms, ec, ct, lookupName);

            return Ok(result);
        }

        //post -c "{\"Values\":{\"c_categoria_id\":\"1\"}}"
        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/view/{viewName}")]
        public async Task<ObjectResult> View([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleExecuteView(auth, app_id, entityName, viewName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/proc/{procName}")]
        public async Task<ObjectResult> Proc([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleExecuteProc(auth, app_id, entityName, procName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/process/{procName}")]
        public async Task<ObjectResult> Process([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleExecuteProcDBStatus(auth, app_id, entityName, procName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/action/{actionName}")]
        public async Task<ObjectResult> Action([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string actionName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);

            var result = await api.HandleExecuteAction(auth, app_id, entityName, actionName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [Authorize(policy: nameof(MicroMPermissionsConstants.MicroMPermissionsPolicy))]
        [HttpPost("{app_id}/{entityName}/import/{import_proc?}")]
        public async Task<ObjectResult> Import([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string? import_proc, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);

            var result = await api.HandleImportData(auth, app_id, entityName, import_proc, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }


        //
        // Public endpoints
        //
        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/get")]
        public async Task<ObjectResult> PublicGet([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";

            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleGetEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }

        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/insert")]
        public async Task<ObjectResult> PublicInsert(
            [FromServices] IAuthenticationProvider auth,
            [FromServices] IMicroMWebAPI api,
            [FromBody] DataWebAPIRequest parms,
            string app_id, string entityName,
            CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleInsertEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }


        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/update")]
        public async Task<ObjectResult> PublicUpdate([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleUpdateEntity(auth, app_id, entityName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }

            return BadRequest("");
        }

        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/delete")]
        public async Task<ObjectResult> PublicDelete([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleDeleteEntity(auth, app_id, entityName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/lookup/{lookupName?}")]
        public async Task<ObjectResult> PublicLookup([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string? lookupName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleLookupEntity(auth, app_id, entityName, parms, ec, ct, lookupName);

            return Ok(result);
        }

        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/view/{viewName}")]
        public async Task<ObjectResult> PublicView([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string viewName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleExecuteView(auth, app_id, entityName, viewName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/proc/{procName}")]
        public async Task<ObjectResult> PublicProc([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleExecuteProc(auth, app_id, entityName, procName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/process/{procName}")]
        public async Task<ObjectResult> PublicProcess([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string procName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);
            var result = await api.HandleExecuteProcDBStatus(auth, app_id, entityName, procName, parms, ec, ct);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

        [AllowAnonymous]
        [PublicEndpoint]
        [HttpPost("{app_id}/public/{entityName}/action/{actionName}")]
        public async Task<ObjectResult> PublicAction([FromServices] IAuthenticationProvider auth, [FromServices] IMicroMWebAPI api, string app_id, string entityName, string actionName, [FromBody] DataWebAPIRequest parms, CancellationToken ct)
        {
            parms.ServerClaims = User.Claims.ToClaimsDictionary();
            parms.ServerClaims[MicroMServerClaimTypes.MicroMUsername] = "public";
            using var ec = await api.CreateDbConnection(app_id, parms.ServerClaims, auth, ct);

            var result = await api.HandleExecuteAction(auth, app_id, entityName, actionName, parms, ec, ct);

            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest("");
        }

    }
}
