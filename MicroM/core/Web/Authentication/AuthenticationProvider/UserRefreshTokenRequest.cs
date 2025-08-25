namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents a request to obtain new access tokens using an existing refresh token.
    /// </summary>
    public class UserRefreshTokenRequest
    {
        /// <summary>
        /// Gets or sets the bearer token presented by the client.
        /// </summary>
        public string Bearer { get; set; } = "";

        /// <summary>
        /// Gets or sets the refresh token used to request new access tokens.
        /// </summary>
        public string RefreshToken { get; set; } = "";
    }
}
