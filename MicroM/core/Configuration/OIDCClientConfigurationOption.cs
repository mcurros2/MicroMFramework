namespace MicroM.Configuration;

public class OIDCClientConfigurationOption
{
    public string ApplicationID { get; set; } = "";
    public string ClientAPPID { get; set; } = "";
    public string URLFrontChannelLogout { get; set; } = "";
    public string URLBackchannelLogout { get; set; } = "";
    public string URLClientJWKS { get; set; } = "";
    public string CertificateUniqueID { get; set; } = "";
    public string APIKey { get; set; } = "";
    public string APISecret { get; set; } = "";
    public string OIDCSubjectPepper { get; set; } = "";
    public List<string> URLAuthorizedRedirects { get; set; } = [];
}
