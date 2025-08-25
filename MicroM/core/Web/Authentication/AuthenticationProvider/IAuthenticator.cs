using MicroM.Configuration;
using MicroM.DataDictionary.Entities.MicromUsers;

namespace MicroM.Web.Authentication;

/// <summary>
/// Defines the contract for authenticators capable of handling login, token refresh,
/// password recovery and related operations.
/// </summary>
public interface IAuthenticator
{
    /// <summary>
    /// Identifier of the email template used for password recovery messages.
    /// </summary>
    public const string AuthenticatorRecoveryEmailTemplateID = "RECOVERY";

    /// <summary>
    /// Placeholder tag within the recovery email template that is replaced with the recovery code.
    /// </summary>
    public const string AuthenticatorRecoveryEmailTemplateCodeTAG = "{RECOVERY_CODE}";

    /// <summary>
    /// Authenticates a user based on the provided login information.
    /// </summary>
    /// <param name="app_config">Application configuration containing authentication options.</param>
    /// <param name="user_login">Credentials supplied by the user.</param>
    /// <param name="ct">Token used to observe cancellation requests.</param>
    /// <returns>The result of the authentication attempt.</returns>
    Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct);

    /// <summary>
    /// Validates the specified refresh token and issues a new token set.
    /// </summary>
    /// <param name="app_config">Application configuration containing authentication options.</param>
    /// <param name="user_id">Identifier of the user requesting the refresh.</param>
    /// <param name="refresh_token">The refresh token to validate.</param>
    /// <param name="local_device_id">Identifier of the device requesting the refresh.</param>
    /// <param name="ct">Token used to observe cancellation requests.</param>
    /// <returns>The refresh operation result.</returns>
    Task<RefreshTokenResult> AuthenticateRefresh(ApplicationOption app_config, string user_id, string refresh_token, string local_device_id, CancellationToken ct);

    /// <summary>
    /// Logs the specified user out of the system.
    /// </summary>
    /// <param name="app_config">Application configuration containing authentication options.</param>
    /// <param name="user_name">Name of the user to log off.</param>
    /// <param name="ct">Token used to observe cancellation requests.</param>
    Task Logoff(ApplicationOption app_config, string user_name, CancellationToken ct);

    /// <summary>
    /// Decrypts the provided collection of server claims.
    /// </summary>
    /// <param name="server_claims">Encrypted claims to decrypt in place.</param>
    void UnencryptClaims(Dictionary<string, object>? server_claims);

    /// <summary>
    /// Sends a password recovery email to the specified user.
    /// </summary>
    /// <param name="app_config">Application configuration containing authentication options.</param>
    /// <param name="user_name">Name of the user requesting password recovery.</param>
    /// <param name="ct">Token used to observe cancellation requests.</param>
    /// <returns>A tuple indicating whether the operation failed and an optional error message.</returns>
    Task<(bool failed, string? error_message)> SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct);

    /// <summary>
    /// Resets the user's password using a previously issued recovery code.
    /// </summary>
    /// <param name="app_config">Application configuration containing authentication options.</param>
    /// <param name="user_name">Name of the user whose password is being reset.</param>
    /// <param name="new_password">The new password to set.</param>
    /// <param name="recovery_code">Recovery code sent to the user.</param>
    /// <param name="ct">Token used to observe cancellation requests.</param>
    /// <returns>A tuple indicating whether the operation failed and an optional error message.</returns>
    Task<(bool failed, string? error_message)> RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct);
}
