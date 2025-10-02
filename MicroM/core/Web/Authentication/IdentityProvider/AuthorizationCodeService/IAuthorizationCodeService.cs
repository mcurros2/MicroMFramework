using MicroM.Configuration;

namespace MicroM.Web.Authentication.SSO
{
    public interface IAuthorizationCodeService
    {
        void ClearAllAuthorizationCodes();
        void ClearAuthorizationCodesForApp(ApplicationOption app);
        void RemoveAuthorizationCodesForClient(ApplicationOption app, string clientId);
        AuthorizationCodeRecord CreateAndStoreAuthorizationCode(ApplicationOption app, string clientId, AuthorizationCodeRecord record);
        AuthorizationCodeRecord? ValidateAndConsumeAuthorizationCode(ApplicationOption app, string code, string clientId, string redirectUri, string? codeVerifier);
    }
}