namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the EmailServiceConfigurationData.
    /// </summary>
    public record EmailServiceConfigurationData
    {
        /// <summary>
        /// c_email_configuration_id; field.
        /// </summary>
        public string? c_email_configuration_id;
        /// <summary>
        /// vc_smtp_host; field.
        /// </summary>
        public string? vc_smtp_host;
        /// <summary>
        /// i_smtp_port; field.
        /// </summary>
        public int i_smtp_port;
        /// <summary>
        /// vc_user_name; field.
        /// </summary>
        public string? vc_user_name;
        /// <summary>
        /// vc_password; field.
        /// </summary>
        public string? vc_password;
        /// <summary>
        /// bt_use_ssl; field.
        /// </summary>
        public bool bt_use_ssl;
        /// <summary>
        /// vc_default_sender_email; field.
        /// </summary>
        public string? vc_default_sender_email;
        /// <summary>
        /// vc_default_sender_name; field.
        /// </summary>
        public string? vc_default_sender_name;
    }
}
