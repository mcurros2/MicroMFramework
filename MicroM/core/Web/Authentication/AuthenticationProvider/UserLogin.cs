namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents credentials supplied by a user attempting to sign in.
    /// </summary>
    public class UserLogin
    {
        /// <summary>
        /// Gets or sets the user name used to authenticate.
        /// </summary>
        public string Username { get; set; } = "";

        /// <summary>
        /// Gets or sets the user's password.
        /// </summary>
        public string Password { get; set; } = "";

        /// <summary>
        /// Gets or sets the identifier of the device initiating the login request.
        /// </summary>
        public string LocalDeviceID { get; set; } = "";
    }

}
