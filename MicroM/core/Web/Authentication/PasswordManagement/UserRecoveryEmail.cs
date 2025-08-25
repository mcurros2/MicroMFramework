namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents a request to send a password recovery email.
    /// </summary>
    public class UserRecoveryEmail
    {
        /// <summary>
        /// Gets or sets the user name that will receive the recovery email.
        /// </summary>
        public string Username { get; set; } = "";
    }
}
