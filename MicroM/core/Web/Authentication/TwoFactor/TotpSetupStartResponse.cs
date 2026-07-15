namespace MicroM.Web.Authentication;

public class TotpSetupStartResponse
{
    public string setup_challenge_id { get; set; } = "";
    public string qr_code_data_url { get; set; } = "";
}
