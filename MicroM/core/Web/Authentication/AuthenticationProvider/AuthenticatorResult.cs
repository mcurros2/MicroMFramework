using MicroM.DataDictionary.Entities.MicromUsers;
using Microsoft.AspNetCore.Identity;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents the outcome of an authentication attempt.
    /// </summary>
    public class AuthenticatorResult
    {
        /// <summary>
        /// Indicates whether the user's account is disabled. Defaults to <see langword="false"/>.
        /// </summary>
        public bool AccountDisabled = false;

        /// <summary>
        /// Indicates whether the user's account is locked. Defaults to <see langword="false"/>.
        /// </summary>
        public bool AccountLocked = false;

        /// <summary>
        /// Result of verifying the supplied password. Initialized to <see cref="PasswordVerificationResult.Failed"/>.
        /// </summary>
        public PasswordVerificationResult PasswordVerificationResult = PasswordVerificationResult.Failed;

        /// <summary>
        /// User login information associated with the authentication attempt, if available.
        /// </summary>
        public LoginData? LoginData = null;

        /// <summary>
        /// Collection of decrypted server-side claims returned by the authenticator.
        /// </summary>
        public Dictionary<string, object> ServerClaims = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Collection of client-side claims to be issued to the caller.
        /// </summary>
        public Dictionary<string, string> ClientClaims = new(StringComparer.OrdinalIgnoreCase);
    }
}
