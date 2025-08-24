namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents the UserRefreshTokenRequest.
    /// </summary>
    public class UserRefreshTokenRequest
    {
        /// <summary>
        /// Gets or sets the "";.
        /// </summary>
        public string Bearer { get; set; } = "";
        /// <summary>
        /// Gets or sets the "";.
        /// </summary>
        public string RefreshToken { get; set; } = "";
    }
}
