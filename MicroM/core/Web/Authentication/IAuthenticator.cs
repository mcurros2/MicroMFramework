using MicroM.Configuration;
using MicroM.DataDictionary.Entities.MicromUsers;

namespace MicroM.Web.Authentication
{

    public interface IAuthenticator
    {
        public const string AuthenticatorRecoveryEmailTemplateID = "RECOVERY";
        public const string AuthenticatorRecoveryEmailTemplateCodeTAG = "{RECOVERY_CODE}";

        public abstract Task<AuthenticatorResult> AuthenticateLogin(ApplicationOption app_config, UserLogin user_login, CancellationToken ct);

        public abstract Task<RefreshTokenResult> AuthenticateRefresh(ApplicationOption app_config, string user_id, string refresh_token, CancellationToken ct);

        public abstract Task Logoff(ApplicationOption app_config, string user_name, CancellationToken ct);

        public abstract void UnencryptClaims(Dictionary<string, object>? server_claims);

        public abstract Task<(bool failed, string? error_message)> SendPasswordRecoveryEmail(ApplicationOption app_config, string user_name, CancellationToken ct);

        public abstract Task<(bool failed, string? error_message)> RecoverPassword(ApplicationOption app_config, string user_name, string new_password, string recovery_code, CancellationToken ct);
    }
}
