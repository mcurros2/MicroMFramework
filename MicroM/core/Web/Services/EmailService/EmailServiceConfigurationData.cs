namespace MicroM.Web.Services
{
    public record EmailServiceConfigurationData
    {
        public string? c_email_configuration_id;
        public string? vc_smtp_host;
        public int i_smtp_port;
        public string? vc_user_name;
        public string? vc_password;
        public bool bt_use_ssl;
        public string? vc_default_sender_email;
        public string? vc_default_sender_name;
    }
}
