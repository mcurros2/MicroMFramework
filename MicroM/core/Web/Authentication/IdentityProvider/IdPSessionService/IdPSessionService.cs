using MicroM.Configuration;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Authentication.SSO;

public class IdPSessionService(ILogger<IdPSessionService> log, IDeviceIdService deviceid_service, IMicroMEncryption encryptor) : IIdPSessionService
{
    public async Task<Guid> CreateSession(ApplicationOption app, string client_app_id, string user_name, CancellationToken ct, string local_device_id = "", Dictionary<string, object>? server_claims = null)
    {
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, server_claims);

        ApplicationOidcActiveSessions new_session = new(dbc, encryptor);

        var session_guid = Guid.NewGuid();
        new_session.Def.c_application_id.Value = client_app_id;
        new_session.Def.vc_username.Value = user_name;
        new_session.Def.c_device_id.Value = local_device_id;
        new_session.Def.ui_oidc_session_guid_id.Value = session_guid;
        await new_session.InsertData(ct);

        return session_guid;
    }

    public async Task RemoveAllSessions(ApplicationOption app, CancellationToken ct, Dictionary<string, object>? server_claims = null)
    {
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, server_claims);

        var sessions = new ApplicationOidcActiveSessions(dbc, encryptor);
        await sessions.ExecuteProc(ct, sessions.Def.aos_deleteAllSessions);
    }

    public async Task RemoveSession(ApplicationOption app, Guid session_guid_id, CancellationToken ct, Dictionary<string, object>? server_claims = null)
    {
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, server_claims);

        ApplicationOidcActiveSessions del_session = new(dbc, encryptor);
        del_session.Def.ui_oidc_session_guid_id.Value = session_guid_id;

        await del_session.ExecuteProc(ct, del_session.Def.aos_deleteSessionGUID);
    }

    public async Task RemoveUserSessions(ApplicationOption app, string user_name, CancellationToken ct, Dictionary<string, object>? server_claims = null)
    {
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, server_claims);

        ApplicationOidcActiveSessions del_session = new(dbc, encryptor);
        del_session.Def.vc_username.Value = user_name;
        await del_session.ExecuteProc(ct, del_session.Def.aos_deleteUserSessions);
    }
}
