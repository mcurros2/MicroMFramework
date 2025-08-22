using MicroM.DataDictionary.Entities.MicromUsers;
using Microsoft.AspNetCore.Identity;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents the AuthenticatorResult.
    /// </summary>
    public class AuthenticatorResult
    {
        /// <summary>
        /// false; field.
        /// </summary>
        public bool AccountDisabled = false;
        /// <summary>
        /// false; field.
        /// </summary>
        public bool AccountLocked = false;
        /// <summary>
        /// PasswordVerificationResult.Failed; field.
        /// </summary>
        public PasswordVerificationResult PasswordVerificationResult = PasswordVerificationResult.Failed;
        /// <summary>
        /// null; field.
        /// </summary>
        public LoginData? LoginData = null;
        /// <summary>
        /// Performs the new operation.
        /// </summary>
        public Dictionary<string, object> ServerClaims = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Performs the new operation.
        /// </summary>
        public Dictionary<string, string> ClientClaims = new(StringComparer.OrdinalIgnoreCase);
    }
}
