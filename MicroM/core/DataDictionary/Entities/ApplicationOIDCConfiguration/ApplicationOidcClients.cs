using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ApplicationOidcClientsDef : EntityDefinition
{
    public ApplicationOidcClientsDef() : base("aoi", nameof(ApplicationOidcClients)) { }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_client_app_id = Column<string>.PK(autonum: true);
    public readonly Column<string> c_api_key_id = Column<string>.FK();

    public readonly Column<string> vc_url_sso_frontchannel_logout = Column<string>.Text(size: 2048);
    public readonly Column<string> vc_url_sso_backchannel_logout = Column<string>.Text(size: 2048);

    // This is for private jwks authentication
    // The URL should resolve and keep the ceritificate in memory when the service starts
    public readonly Column<string?> vc_url_client_jwks = Column<string?>.Text(size: 2048);
    public readonly Column<string?> vc_certificate_unique_id = Column<string?>.Text(size: 2048);

    // This is the pepper to use when creating the subject claim for tokens issued to this client
    public readonly Column<string> vc_oidc_subject_pepper = Column<string>.Text(size: 2048, encrypted: true);

    // These are stored encrypted so the user can copy them in the control panel
    public readonly Column<string?> vc_apikey = Column<string?>.Text(size: 2048, encrypted: true, fake: true);
    public readonly Column<string?> vc_secret = Column<string?>.Text(size: 2048, encrypted: true, fake: true);

    public readonly Column<string> vc_url_authorized_redirects = Column<string>.Text(size: 0, isArray: true, fake: true);

    // These is used in the UI to explicitly recreate the api key and secret
    public readonly Column<bool?> b_change_secret = new(fake: true);

    public readonly ViewDefinition aoi_brwStandard = new(nameof(c_application_id), nameof(c_client_app_id));

    public readonly EntityForeignKey<Applications, ApplicationOidcClients> FKApplicationsClients = new();
    public readonly EntityForeignKey<MicromApplicationApiKeys, ApplicationOidcClients> FKApplicationClientsApiKeys = new();

}

public class ApplicationOidcClients : Entity<ApplicationOidcClientsDef>
{
    public ApplicationOidcClients() : base() { }
    public ApplicationOidcClients(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    public override Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        this.Def.vc_apikey.Value = Guid.NewGuid().ToString();
        this.Def.vc_secret.Value = CryptClass.CreateRandomPassword();
        this.Def.vc_oidc_subject_pepper.Value = CryptClass.CreateRandomPassword();

        return base.InsertData(ct, throw_dbstat_exception, options, server_claims, api, app_id);
    }

    public override Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        if (this.Def.b_change_secret.Value == true)
        {
            this.Def.vc_apikey.Value = Guid.NewGuid().ToString();
            this.Def.vc_secret.Value = CryptClass.CreateRandomPassword();
        }

        return base.UpdateData(ct, throw_dbstat_exception, options, server_claims, api, app_id);
    }

}


