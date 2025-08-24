

using MicroM.Web.Authentication;

namespace MicroM.DataDictionary.Entities.MicromUsers
{
    /// <summary>
    /// Result returned after a successful login.
    /// </summary>
    public record LoginResult
    {
        /// <summary>
        /// Email associated with the user.
        /// </summary>
        public string? email { get; set; }

        /// <summary>
        /// Username of the authenticated user.
        /// </summary>
        public string username { get; set; } = "";

        /// <summary>
        /// Refresh token issued for the session.
        /// </summary>
        public string? refresh_token { get; set; }

        /// <summary>
        /// Client-specific claims included in the login response.
        /// </summary>
        public Dictionary<string, string> client_claims { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Result of the authenticator challenge, if any.
        /// </summary>
        public AuthenticatorResult authenticator_result { get; set; } = new();
    }
}
