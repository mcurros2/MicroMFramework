namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Defines claim type names issued to clients.
    /// </summary>
    public class MicroMClientClaimTypes
    {
        /// <summary>
        /// Claim type for the user's name.
        /// </summary>
        public const string username = nameof(username);

        /// <summary>
        /// Claim type for the user's email address.
        /// </summary>
        public const string useremail = nameof(useremail);
    }
}
