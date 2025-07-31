namespace MicroM.Web.Authentication
{
    public class UserRefreshTokenRequest
    {
        public string Bearer { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}
