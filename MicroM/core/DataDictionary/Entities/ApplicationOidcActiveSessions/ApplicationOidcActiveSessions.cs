using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace MicroM.DataDictionary.Entities;

public record OidcSessionItem
{
    public string c_application_id = "";
    public string c_user_id = "";
    public string c_device_id = "";
    public string vc_username = "";
    public string vc_oidc_session_id = "";
    public string? vc_oidc_sub;
    public string? vc_oidc_refreshtoken;
    public DateTime? dt_refresh_expiration;
};

public class ApplicationOidcActiveSessionsDef : EntityDefinition
{
    public ApplicationOidcActiveSessionsDef() : base("aos", nameof(ApplicationOidcActiveSessions)) { }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_user_id = Column<string>.PK(); // UserID is different at the IdP and the client app, treat as local
    public readonly Column<string> c_device_id = Column<string>.PK();

    public readonly Column<string?> vc_username = Column<string?>.Text();

    public readonly Column<string> vc_oidc_session_id = Column<string>.Text();
    public readonly Column<string?> vc_oidc_sub = Column<string?>.Text(nullable: true);

    public readonly Column<string?> vc_oidc_refreshtoken = Column<string?>.Text(size: 2048, nullable: true, encrypted: true);
    public readonly Column<DateTime?> dt_refresh_expiration = new(nullable: true);

    public readonly ProcedureDefinition aos_deleteAllSessions = new();
    public readonly ProcedureDefinition aos_deleteSessionsBySUB = new(nameof(vc_oidc_sub));

    // Get all sessions at the IdP for the user that has the given OIDC Session ID, use at the IdP side
    public readonly ProcedureDefinition aos_getUserSessionsBySID = new(readonly_locks: true, nameof(c_application_id), nameof(vc_oidc_session_id));

    public readonly ProcedureDefinition aos_getSessionsByUser = new(readonly_locks: true, nameof(vc_username));
    public readonly ProcedureDefinition aos_getSessionsBySUB = new(readonly_locks: true, nameof(vc_oidc_sub));

    public readonly ProcedureDefinition aos_getSessionByRefreshToken = new(readonly_locks: true, nameof(c_application_id), nameof(vc_oidc_refreshtoken));
    public readonly ProcedureDefinition aos_getSessionBySID = new(readonly_locks: true, nameof(c_application_id), nameof(vc_oidc_session_id));

    public readonly ProcedureDefinition aos_getUsernameFromSIDorSUB = new(readonly_locks: true, nameof(c_application_id), nameof(vc_oidc_session_id), nameof(vc_oidc_sub));
    public readonly ProcedureDefinition aos_getSUBFromSID = new(readonly_locks: true, nameof(c_application_id), nameof(vc_oidc_session_id));

    // No referential integrity for application.
    // When acting as IdPServer sessions are maintained here
    // When acting as a client, sessions are maintained to link with IdP.

    public readonly EntityForeignKey<MicromUsers, ApplicationOidcActiveSessions> FKUsers = new();

    public readonly EntityUniqueConstraint UNApplicationSession = new(keys: [nameof(c_application_id), nameof(vc_oidc_session_id)]);
    public readonly EntityUniqueConstraint UNRefreshToken = new(keys: [nameof(c_application_id), nameof(vc_oidc_refreshtoken)]);

    public readonly EntityIndex IDXUserSessions = new(keys: [nameof(vc_username)]);
    public readonly EntityIndex IDXSub = new(keys: [nameof(vc_oidc_sub)]);
}

/// <summary>
/// This entity is used to persist sessions created by an Identity Provider (IdP) and sessions created by an OIDC client application.
/// The table is created in the IdP database server and separatetly in each client application database server.
/// IdP Application may or may not reside in the same server as the client applications.
/// IdP registers sessions here when a user authenticates and receives an OIDC session id (sid) for each client application.
/// Client applications register the sessions on their own DB when they receive the sid from the IdP.
/// </summary>
public class ApplicationOidcActiveSessions : Entity<ApplicationOidcActiveSessionsDef>
{
    public ApplicationOidcActiveSessions() : base() { }
    public ApplicationOidcActiveSessions(string? schema_name) : base(schema_name) { }
    public ApplicationOidcActiveSessions(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    public static string GetDerivedSub(string client_app_id, string idp_user_id, string subject_pepper)
    {
        var data = Encoding.UTF8.GetBytes($"{client_app_id}:{idp_user_id}");
        byte[] hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(subject_pepper), data);
        return Base64UrlEncoder.Encode(hash);
    }

    /// <summary>
    /// This method creates or update an OIDC session record for a user in the Identity Provider (IdP) database.
    /// </summary>
    public static async Task<string> CreateOrUpdateIdPSession(
        ApplicationOption app,
        IEntityClient ec,
        string client_app_id,
        string? username,
        string idp_user_id,
        string device_id,
        string subject_pepper,
        string session_id,
        IMicroMEncryption? encryptor,
        CancellationToken ct,
        string? idp_refresh_token = null,
        DateTime? refresh_expiration_utc = null
        )
    {

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            var new_session = new ApplicationOidcActiveSessions(ec, encryptor, schema_name: app.SchemaConfiguration.DDSchema);

            new_session.Def.c_application_id.Value = client_app_id;
            new_session.Def.c_user_id.Value = idp_user_id;
            new_session.Def.c_device_id.Value = device_id;

            new_session.Def.vc_username.Value = username;

            // Derive pairwise sub using client_app_id + idp_user_id, hardened with pepper (HMAC-SHA256)
            var sub_hash = GetDerivedSub(client_app_id, idp_user_id, subject_pepper);
            new_session.Def.vc_oidc_sub.Value = sub_hash;

            new_session.Def.vc_oidc_session_id.Value = session_id;
            new_session.Def.vc_oidc_refreshtoken.Value = idp_refresh_token;
            new_session.Def.dt_refresh_expiration.Value = refresh_expiration_utc;

            await new_session.UpdateData(ct);

            return sub_hash;
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

    }

    /// <summary>
    /// This method creates or updates an OIDC session record for a user in a client application database (at the client app side).
    /// </summary>
    public static async Task<DBStatusResult> CreateOrUpdateExternalSignInSession(
        ApplicationOption app,
        string app_id,
        string username,
        string user_id,
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

            var sess = new ApplicationOidcActiveSessions(ec, encryptor, schema_name: app.SchemaConfiguration.DDSchema);

            sess.Def.c_application_id.Value = app_id;
            sess.Def.c_user_id.Value = user_id;
            sess.Def.c_device_id.Value = device_id;

            sess.Def.vc_username.Value = username;
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

    internal async Task<OidcSessionItem> MapSessionRecord(IValueReader fv, string[] headers, string[] typeInfo, CancellationToken ct)
    {
        var result = new OidcSessionItem
        {
            // Column names exactly as table/proc output; use those for GetFieldValueAsync
            c_application_id = await fv.GetFieldValueAsync<string>(nameof(Def.c_application_id), ct),
            c_user_id = await fv.GetFieldValueAsync<string>(nameof(Def.c_user_id), ct),
            c_device_id = await fv.GetFieldValueAsync<string>(nameof(Def.c_device_id), ct),
            vc_username = await fv.GetFieldValueAsync<string>(nameof(Def.vc_username), ct),
            vc_oidc_session_id = await fv.GetFieldValueAsync<string>(nameof(Def.vc_oidc_session_id), ct),
            vc_oidc_refreshtoken = await fv.GetFieldValueAsync<string?>(nameof(Def.vc_oidc_refreshtoken), ct),
            dt_refresh_expiration = await fv.GetFieldValueAsync<DateTime?>(nameof(Def.dt_refresh_expiration), ct),
            vc_oidc_sub = await fv.GetFieldValueAsync<string?>(nameof(Def.vc_oidc_sub), ct)
        };

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

    public static async Task<List<OidcSessionItem>> GetSessionsBySid(ApplicationOption app, IEntityClient ec, string app_id, string sid, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        var entity = new ApplicationOidcActiveSessions(ec, encryptor, schema_name: app.SchemaConfiguration.DDSchema);
        List<OidcSessionItem>? result = [];

        bool should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {

            await ec.Connect(ct);

            entity.Def.c_application_id.Value = app_id;
            entity.Def.vc_oidc_session_id.Value = sid;

            result = await entity.Data.ExecuteProc(entity.Def.aos_getUserSessionsBySID, ct, mapper: entity.MapSessionRecord);

        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

        return result;
    }

    public static async Task<List<OidcSessionItem>> GetSessionsByUsername(ApplicationOption app, IEntityClient ec, string username, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        var entity = new ApplicationOidcActiveSessions(ec, encryptor, schema_name: app.SchemaConfiguration.DDSchema);
        List<OidcSessionItem>? result = [];

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            entity.Def.vc_username.Value = username;
            result = await entity.Data.ExecuteProc(entity.Def.aos_getSessionsByUser, ct, mapper: entity.MapSessionRecord);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
        return result;
    }

    public static async Task<List<OidcSessionItem>> GetSessionsBySubject(ApplicationOption app, IEntityClient ec, string sub, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        var entity = new ApplicationOidcActiveSessions(ec, encryptor, schema_name: app.SchemaConfiguration.DDSchema);
        List<OidcSessionItem>? result = [];

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            entity.Def.vc_oidc_sub.Value = sub;
            result = await entity.Data.ExecuteProc(entity.Def.aos_getSessionsBySUB, ct, mapper: entity.MapSessionRecord);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
        return result;
    }

    public static async Task<OidcSessionItem?> GetSessionByRefreshToken(ApplicationOption app, IEntityClient ec, string client_id, string refresh_token, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        var entity = new ApplicationOidcActiveSessions(ec, encryptor, schema_name: app.SchemaConfiguration.DDSchema);
        List<OidcSessionItem>? result = [];

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            entity.Def.c_application_id.Value = client_id;

            // IMPORTANT: column is encrypted at rest; pass encrypted value for lookup.
            entity.Def.vc_oidc_refreshtoken.Value = encryptor != null ? encryptor.Encrypt(refresh_token) : refresh_token;

            result = await entity.Data.ExecuteProc(entity.Def.aos_getSessionByRefreshToken, ct, mapper: entity.MapSessionRecord);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
        return result.Count > 0 ? result[0] : null;
    }

    public static async Task<OidcSessionItem?> GetSessionBySID(ApplicationOption app, IEntityClient ec, string client_id, string sid, CancellationToken ct, IMicroMEncryption? encryptor = null)
    {
        var entity = new ApplicationOidcActiveSessions(ec, encryptor, schema_name: app.SchemaConfiguration.DDSchema);
        List<OidcSessionItem>? result = [];

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            entity.Def.c_application_id.Value = client_id;
            entity.Def.vc_oidc_session_id.Value = sid;

            result = await entity.Data.ExecuteProc(entity.Def.aos_getSessionBySID, ct, mapper: entity.MapSessionRecord);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
        return result.Count > 0 ? result[0] : null;
    }

    public static async Task<string?> GetUsernameFromSIDorSUB(ApplicationOption app, IEntityClient ec, string app_id, string? sid, string? sub, CancellationToken ct)
    {
        var entity = new ApplicationOidcActiveSessions(ec, schema_name: app.SchemaConfiguration.DDSchema);
        string? result = null;

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            entity.Def.c_application_id.Value = app_id;
            entity.Def.vc_oidc_sub.Value = sub;
            entity.Def.vc_oidc_session_id.Value = sid ?? "";
            result = await entity.Data.ExecuteProcSingleColumn<string>(entity.Def.aos_getUsernameFromSIDorSUB, ct);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
        return result;
    }

    public static async Task<string?> GetSUBFromSID(ApplicationOption app, IEntityClient ec, string app_id, string sid, CancellationToken ct)
    {
        var entity = new ApplicationOidcActiveSessions(ec, schema_name: app.SchemaConfiguration.DDSchema);
        string? result = null;

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            entity.Def.c_application_id.Value = app_id;
            entity.Def.vc_oidc_session_id.Value = sid;
            result = await entity.Data.ExecuteProcSingleColumn<string>(entity.Def.aos_getSUBFromSID, ct);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
        return result;
    }

    public static async Task<DBStatusResult> DeleteSessionsBySUB(ApplicationOption app, IEntityClient ec, string sub, CancellationToken ct)
    {
        var entity = new ApplicationOidcActiveSessions(ec, schema_name: app.SchemaConfiguration.DDSchema);

        var should_close = !(ec.ConnectionState == ConnectionState.Open);
        try
        {
            await ec.Connect(ct);
            entity.Def.vc_oidc_sub.Value = sub;
            return await entity.Data.ExecuteProcDBStatus(entity.Def.aos_deleteSessionsBySUB, ct);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

}
