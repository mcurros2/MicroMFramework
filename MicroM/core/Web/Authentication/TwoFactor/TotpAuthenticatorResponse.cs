namespace MicroM.Web.Authentication;

public class TotpAuthenticatorResponse
{
    public string authenticator_id { get; set; } = "";
    public string authenticator_name { get; set; } = "";
}

public class TotpAuthenticatorsResponse
{
    public List<TotpAuthenticatorResponse> authenticators { get; set; } = [];
}
