using MicroM.Configuration;
using MicroM.DataDictionary.Entities.MicromUsers;

namespace MicroM.Web.Authentication;

/// <summary>
/// Represents the IAuthenticator.
/// </summary>
public interface IAuthenticator
{
    /// <summary>
    /// "RECOVERY"; field.
    /// </summary>
    public const string AuthenticatorRecoveryEmailTemplateID = "RECOVERY";
    /// <summary>
    /// Gets or sets the "RECOVERY_CODE}";.
    /// </summary>
    public const string AuthenticatorRecoveryEmailTemplateCodeTAG = "{RECOVERY_CODE}";

    /// <summary>
    /// Performs the AuthenticateLogin operation.
    /// </summary>
    public abstract Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct);

    /// <summary>
    /// Performs the AuthenticateRefresh operation.
    /// </summary>
    public abstract Task<RefreshTokenResult> AuthenticateRefresh(ApplicationOption app_config, string user_id, string refresh_token, string local_device_id, CancellationToken ct);

    /// <summary>
    /// Performs the Logoff operation.
    /// </summary>
    public abstract Task Logoff(ApplicationOption app_config, string user_name, CancellationToken ct);

    /// <summary>
    /// Performs the UnencryptClaims operation.
    /// </summary>
    public abstract void UnencryptClaims(Dictionary<string, object>? server_claims);

    /// <summary>
    /// Performs the Task< operation.
    /// </summary>
    public abstract Task<(bool failed, string? error_message)> SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct);

    /// <summary>
    /// Performs the Task< operation.
    /// </summary>
    public abstract Task<(bool failed, string? error_message)> RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct);
}
