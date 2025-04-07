namespace MicroM.Web.Authentication
{
    public class UserRecoverPassword
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string RecoveryCode { get; set; } = "";
    }
}
