using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromApplicationApiKeysDef : EntityDefinition
{
    public MicromApplicationApiKeysDef() : base("mak", nameof(MicromApplicationApiKeys)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdate; }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_api_key_id = Column<string>.PK(autonum: true);

    // These are stored encrypted so the user can copy them in the control panel
    public readonly Column<string> vc_apikey = Column<string>.Text(size: 2048, encrypted: true);
    public readonly Column<string> vc_secret = Column<string>.Text(size: 2048, encrypted: true);

    // These is used in the UI to explicitly recreate the api key and secret
    public readonly Column<bool?> b_change_secret = new(fake: true);

    public readonly ViewDefinition mak_brwStandard = new(nameof(c_application_id), nameof(c_api_key_id));
    public readonly ProcedureDefinition mak_getByAPIKey = new(readonly_locks: true, nameof(c_application_id), nameof(c_api_key_id));

    public readonly EntityForeignKey<Applications, MicromApplicationApiKeys> FKApplications = new();
    public readonly EntityUniqueConstraint UNApplicationApiKey = new(keys: [nameof(c_application_id), nameof(vc_apikey)]);

}

public class MicromApplicationApiKeys : Entity<MicromApplicationApiKeysDef>
{
    public MicromApplicationApiKeys() : base() { }
    public MicromApplicationApiKeys(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    public override Task<DBStatusResult> InsertData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        this.Def.vc_apikey.Value = new Guid().ToString();
        this.Def.vc_secret.Value = CryptClass.CreateRandomPassword();

        return base.InsertData(ct, throw_dbstat_exception, options, server_claims, api, app_id);
    }

    public override Task<DBStatusResult> UpdateData(CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null, IWebAPIServices? api = null, string? app_id = null)
    {
        if (this.Def.b_change_secret.Value == true)
        {
            this.Def.vc_apikey.Value = new Guid().ToString();
            this.Def.vc_secret.Value = CryptClass.CreateRandomPassword();
        }

        return base.UpdateData(ct, throw_dbstat_exception, options, server_claims, api, app_id);
    }

    public async Task<bool> GetByAPIKey(CancellationToken ct)
    {
        var result = await this.ExecuteProc(this.Def.mak_getByAPIKey, ct);

        bool ret = Data.MapGetColumns(result);

        return ret;
    }

    public async Task<bool> ValidateSecret(IEntityClient ec, string app_id, string api_key, string secret, CancellationToken ct)
    {
        var mak = new MicromApplicationApiKeys(ec);

        mak.Def.c_application_id.Value = app_id;
        mak.Def.vc_apikey.Value = api_key;

        if (await GetByAPIKey(ct))
        {
            if (mak.Def.vc_secret.Value == secret)
            {
                return true;
            }
        }

        return false;
    }

}


