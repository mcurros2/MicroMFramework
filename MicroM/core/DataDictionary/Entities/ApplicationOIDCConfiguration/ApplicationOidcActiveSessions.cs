using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary.Entities;

public record OidcSessionItem
{
    public string c_application_id = "";
    public string vc_username = "";
    public string c_device_id = "";
    public string vc_oidc_session_id = "";
    public string? vc_oidc_sub;
    public string? vc_oidc_refreshtoken;
    public DateTime? dt_refresh_expiration;
};

public class ApplicationOidcActiveSessionsDef : EntityDefinition
{
    public ApplicationOidcActiveSessionsDef() : base("aos", nameof(ApplicationOidcActiveSessions)) { }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> vc_username = Column<string>.Text(column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.Get | ColumnFlags.PK);
    public readonly Column<string> c_device_id = Column<string>.PK();

    public readonly Column<string> vc_oidc_session_id = Column<string>.Text();
    public readonly Column<string?> vc_oidc_sub = Column<string?>.Text(nullable: true);

    public readonly Column<string?> vc_oidc_refreshtoken = new(sql_type: SqlDbType.VarChar, size: 2048, nullable: true, encrypted: true);
    public readonly Column<DateTime?> dt_refresh_expiration = new(nullable: true);

    public readonly ProcedureDefinition aos_deleteUserSessions = new(nameof(vc_username));
    public readonly ProcedureDefinition aos_deleteAllSessions = new();
    public readonly ProcedureDefinition aos_deleteSessionSID = new(nameof(vc_oidc_session_id));

    public readonly ProcedureDefinition aos_getSessionsBySID = new(readonly_locks: true, nameof(c_application_id), nameof(vc_oidc_session_id));
    public readonly ProcedureDefinition aos_getSessionsByUser = new(readonly_locks: true, nameof(vc_username));
    public readonly ProcedureDefinition aos_getSessionsBySUB = new(readonly_locks: true, nameof(vc_oidc_sub));

    // No referential integrity for application.
    // users and devices are left out intentionally to be orphaned if the user or device is deleted
    // When acting as IdPServer sessions are maintained here
    // When acting as a client, sessions are maintained to link with IdP.

    public readonly EntityForeignKey<MicromUsers, ApplicationOidcActiveSessions> FKUsers = new();

    public readonly EntityUniqueConstraint IDXApplicationSession = new(keys: [nameof(c_application_id), nameof(vc_oidc_session_id)]);
    public readonly EntityIndex IDXUserSessions = new(keys: [nameof(vc_username)]);
    public readonly EntityIndex IDXSub = new(keys: [nameof(vc_oidc_sub)]);
}

public class ApplicationOidcActiveSessions : Entity<ApplicationOidcActiveSessionsDef>
{
    public ApplicationOidcActiveSessions() : base() { }
    public ApplicationOidcActiveSessions(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    public static async Task<DBStatusResult> UpdateSession(
        string app_id,
        string username,
        string device_id,
        string session_id,
        string? subject,
        string? idp_refresh_token,
        DateTime? refresh_expiration_utc,
        IEntityClient ec,
        IMicroMEncryption? encryptor,
        CancellationToken ct)
    {
        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            var sess = new ApplicationOidcActiveSessions(ec, encryptor);

            sess.Def.c_application_id.Value = app_id;
            sess.Def.vc_username.Value = username;
            sess.Def.c_device_id.Value = device_id;
            sess.Def.vc_oidc_session_id.Value = session_id;
            sess.Def.vc_oidc_sub.Value = string.IsNullOrWhiteSpace(subject) ? null : subject;
            sess.Def.vc_oidc_refreshtoken.Value = idp_refresh_token;
            sess.Def.dt_refresh_expiration.Value = refresh_expiration_utc;

            return await sess.UpdateData(ct);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    internal async Task<OidcSessionItem> MapSessionRecord(IGetFieldValue fv, string[] headers, CancellationToken ct)
    {
        var result = new OidcSessionItem();
        // Column names exactly as table/proc output; use those for GetFieldValueAsync
        result.c_application_id = await fv.GetFieldValueAsync<string>(nameof(Def.c_application_id), ct);
        result.vc_username = await fv.GetFieldValueAsync<string>(nameof(Def.vc_username), ct);
        result.c_device_id = await fv.GetFieldValueAsync<string>(nameof(Def.c_device_id), ct);
        result.vc_oidc_session_id = await fv.GetFieldValueAsync<string>(nameof(Def.vc_oidc_session_id), ct);
        result.vc_oidc_refreshtoken = await fv.GetFieldValueAsync<string?>(nameof(Def.vc_oidc_refreshtoken), ct);
        result.dt_refresh_expiration = await fv.GetFieldValueAsync<DateTime?>(nameof(Def.dt_refresh_expiration), ct);
        result.vc_oidc_sub = await fv.GetFieldValueAsync<string?>(nameof(Def.vc_oidc_sub), ct);

        // Decrypt refresh token if provided & encryptor available
        if (!string.IsNullOrEmpty(result.vc_oidc_refreshtoken) && Encryptor != null)
        {
            try
            {
                result.vc_oidc_refreshtoken = Encryptor.Decrypt(result.vc_oidc_refreshtoken);
            }
            catch
            {
                // swallow decryption errors; keep encrypted form
            }
        }

        return result;
    }

    public static async Task<List<OidcSessionItem>> GetSessionsBySid(IEntityClient ec, string app_id, string sid, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        var entity = new ApplicationOidcActiveSessions(ec, encryptor);
        List<OidcSessionItem>? result = [];

        bool should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {

            await ec.Connect(ct);

            entity.Def.c_application_id.Value = app_id;
            entity.Def.vc_oidc_session_id.Value = sid;

            result = await entity.Data.ExecuteProc<OidcSessionItem>(ct, entity.Def.aos_getSessionsBySID, mapper: entity.MapSessionRecord);

        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

        return result;
    }

    public static async Task<List<OidcSessionItem>> GetSessionsByUsername(IEntityClient ec, string username, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        var entity = new ApplicationOidcActiveSessions(ec, encryptor);
        List<OidcSessionItem>? result = [];

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            entity.Def.vc_username.Value = username;
            result = await entity.Data.ExecuteProc<OidcSessionItem>(ct, entity.Def.aos_getSessionsByUser, mapper: entity.MapSessionRecord);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
        return result;
    }

    public static async Task<List<OidcSessionItem>> GetSessionsBySubject(IEntityClient ec, string sub, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        var entity = new ApplicationOidcActiveSessions(ec, encryptor);
        List<OidcSessionItem>? result = [];

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            entity.Def.vc_oidc_sub.Value = sub;
            result = await entity.Data.ExecuteProc<OidcSessionItem>(ct, entity.Def.aos_getSessionsBySUB, mapper: entity.MapSessionRecord);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
        return result;
    }

}
