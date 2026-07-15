using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

using MicroM.Configuration;

namespace MicroM.DataDictionary.Entities;

public class MicromUsersAuthenticatorsDef : EntityDefinition
{
    public MicromUsersAuthenticatorsDef() : base("uau", nameof(MicromUsersAuthenticators)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_user_id = Column<string>.PK();
    public readonly Column<string> c_authenticator_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_authenticator_name = Column<string>.Text();
    public readonly Column<string> vc_totp_secret = Column<string>.Text(size: 2048, encrypted: true, nullable: true);

    public readonly ViewDefinition uau_brwStandard = new(nameof(c_user_id), nameof(c_authenticator_id));

    public readonly EntityForeignKey<MicromUsers, MicromUsersAuthenticators> FKMicromUsers = new();

    public readonly ProcedureDefinition uau_getByUser = new(readonly_locks: true, nameof(c_user_id));
    public readonly ProcedureDefinition uau_countByUser = new(readonly_locks: true, nameof(c_user_id));
    public readonly ProcedureDefinition uau_insertConfirmed = new(nameof(c_user_id), nameof(vc_authenticator_name), nameof(vc_totp_secret));
    public readonly ProcedureDefinition uau_delete = new(nameof(c_user_id), nameof(c_authenticator_id));
    public readonly ProcedureDefinition uau_deleteAll = new(nameof(c_user_id));
}

public class MicromUsersAuthenticators : Entity<MicromUsersAuthenticatorsDef>
{
    public MicromUsersAuthenticators() : base() { }
    public MicromUsersAuthenticators(string? schema_name) : base(schema_name) { }
    public MicromUsersAuthenticators(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    public static async Task<List<MicromUserAuthenticatorData>> GetByUser(ApplicationOption app, string userId, IEntityClient ec, CancellationToken ct)
    {
        MicromUsersAuthenticators entity = new(ec, schema_name: app.SchemaConfiguration.DDSchema);
        entity.Def.c_user_id.Value = userId;
        return await entity.Data.ExecuteProc<MicromUserAuthenticatorData>(entity.Def.uau_getByUser, ct, mode: AutoMapperMode.ByNameLaxNotThrow);
    }

    public static async Task<int> CountByUser(ApplicationOption app, string userId, IEntityClient ec, CancellationToken ct)
    {
        MicromUsersAuthenticators entity = new(ec, schema_name: app.SchemaConfiguration.DDSchema);
        entity.Def.c_user_id.Value = userId;
        var result = await entity.Data.ExecuteProcSingleRow<MicromUserAuthenticatorCount>(entity.Def.uau_countByUser, ct, mode: AutoMapperMode.ByNameLaxNotThrow);
        return result?.authenticator_count ?? 0;
    }

    public static async Task<DBStatusResult> InsertConfirmed(ApplicationOption app, string userId, string authenticatorName, string totpSecret, IEntityClient ec, CancellationToken ct)
    {
        MicromUsersAuthenticators entity = new(ec, schema_name: app.SchemaConfiguration.DDSchema);
        var proc = entity.Def.uau_insertConfirmed;
        proc[nameof(MicromUsersAuthenticatorsDef.c_user_id)].ValueObject = userId;
        proc[nameof(MicromUsersAuthenticatorsDef.vc_authenticator_name)].ValueObject = authenticatorName;
        proc[nameof(MicromUsersAuthenticatorsDef.vc_totp_secret)].ValueObject = totpSecret;
        return await entity.Data.ExecuteProcDBStatus(proc, ct, set_parms_from_columns: false);
    }

    public static async Task<DBStatusResult> Delete(ApplicationOption app, string userId, string authenticatorId, IEntityClient ec, CancellationToken ct)
    {
        MicromUsersAuthenticators entity = new(ec, schema_name: app.SchemaConfiguration.DDSchema);
        var proc = entity.Def.uau_delete;
        proc[nameof(MicromUsersAuthenticatorsDef.c_user_id)].ValueObject = userId;
        proc[nameof(MicromUsersAuthenticatorsDef.c_authenticator_id)].ValueObject = authenticatorId;
        return await entity.Data.ExecuteProcDBStatus(proc, ct, set_parms_from_columns: false);
    }

    public static async Task<DBStatusResult> DeleteAll(ApplicationOption app, string userId, IEntityClient ec, CancellationToken ct)
    {
        MicromUsersAuthenticators entity = new(ec, schema_name: app.SchemaConfiguration.DDSchema);
        entity.Def.c_user_id.Value = userId;
        return await entity.Data.ExecuteProcDBStatus(entity.Def.uau_deleteAll, ct);
    }

}

public class MicromUserAuthenticatorData
{
    public string user_id { get; set; } = "";
    public string authenticator_id { get; set; } = "";
    public string authenticator_name { get; set; } = "";
    public string? totp_secret { get; set; }
}

public class MicromUserAuthenticatorCount
{
    public int authenticator_count { get; set; }
}
