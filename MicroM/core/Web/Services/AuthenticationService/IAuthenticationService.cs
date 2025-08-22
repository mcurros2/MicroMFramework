using MicroM.DataDictionary.Entities.MicromUsers;
using MicroM.Web.Authentication;

namespace MicroM.Web.Services;

/// <summary>
/// Represents the IAuthenticationService.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Performs the Task< operation.
    /// </summary>
    public Task<(LoginResult? user_data, TokenResult? token_result)> HandleLogin(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserLogin user_login, Dictionary<string, object> server_claims, CancellationToken ct);

    /// <summary>
    /// Performs the Task< operation.
    /// </summary>
    public Task<(RefreshTokenResult? result, TokenResult? token_result)> HandleRefreshToken(IAuthenticationProvider auth, WebAPIJsonWebTokenHandler jwt_handler, string app_id, UserRefreshTokenRequest refreshRequest, CancellationToken ct);

    /// <summary>
    /// Performs the HandleLogoff operation.
    /// </summary>
    public Task HandleLogoff(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct);

    /// <summary>
    /// Performs the Task< operation.
    /// </summary>
    public Task<(bool failed, string? error_message)> HandleSendRecoveryEmail(IAuthenticationProvider auth, string app_id, string user_name, CancellationToken ct);

    /// <summary>
    /// Performs the Task< operation.
    /// </summary>
    public Task<(bool failed, string? error_message)> HandleRecoverPassword(IAuthenticationProvider auth, string app_id, string user_name, string new_password, string recovery_code, CancellationToken ct);

}
