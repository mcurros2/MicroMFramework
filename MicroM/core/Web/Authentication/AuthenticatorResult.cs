using MicroM.DataDictionary.Entities.MicromUsers;
using Microsoft.AspNetCore.Identity;

namespace MicroM.Web.Authentication
{
    public class AuthenticatorResult
    {
        public bool AccountDisabled = false;
        public bool AccountLocked = false;
        public PasswordVerificationResult PasswordVerificationResult = PasswordVerificationResult.Failed;
        public LoginData? LoginData = null;
        public Dictionary<string, object> ServerClaims = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ClientClaims = new(StringComparer.OrdinalIgnoreCase);
    }
}
