using MicroM.Configuration;
using MicroM.Core;

namespace MicroM.Web.Authentication;

public interface IAuthenticator
{
    public const string AuthenticatorRecoveryEmailTemplateID = "RECOVERY";
    public const string AuthenticatorRecoveryEmailTemplateCodeTAG = "{RECOVERY_CODE}";

    public abstract Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct);

    public abstract Task<RefreshTokenResult> AuthenticateRefresh(ApplicationOption app_config, string user_id, string refresh_token, string local_device_id, CancellationToken ct);

    public abstract Task Logoff(ApplicationOption app_config, string user_name, CancellationToken ct);

    public abstract void UnencryptClaims(Dictionary<string, object>? server_claims);

    public abstract Task<ResultWithStatus<bool, string>> SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct);

    public abstract Task<ResultWithStatus<bool, string>> RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct);

    Task<ExternalSignInResult> HandleExternalSignIn(ApplicationOption app, ExternalIdentity identity, string deviceId, CancellationToken ct);
}
