namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents the data required to reset a user's password using a recovery code.
    /// </summary>
    public class UserRecoverPassword
    {
        /// <summary>
        /// Gets or sets the user name associated with the recovery request.
        /// </summary>
        public string Username { get; set; } = "";
        /// <summary>
        /// Gets or sets the new password to assign to the user.
        /// </summary>
        public string Password { get; set; } = "";
        /// <summary>
        /// Gets or sets the recovery code that authorizes the password change.
        /// </summary>
        public string RecoveryCode { get; set; } = "";
    }
}
