using MicroM.Configuration;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace MicroM.Web.Authentication.SSO;

public class OIDCSessionService(ILogger<OIDCSessionService> log, IDeviceIdService deviceid_service, IMicroMEncryption encryptor) : IOIDCSessionService
{
    public async Task<string> CreateSession(ApplicationOption app, string client_app_id, string user_name, CancellationToken ct, string local_device_id = "", Dictionary<string, object>? server_claims = null)
    {
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, server_claims);

        ApplicationOidcActiveSessions new_session = new(dbc, encryptor);

        var session_id = Guid.NewGuid().ToString();
        new_session.Def.c_application_id.Value = client_app_id;
        new_session.Def.vc_username.Value = user_name;
        // create a new sub derived form user_name and assign to vc_oidc_sub
        new_session.Def.vc_oidc_sub.Value = Base64UrlEncoder.Encode(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(user_name)));
        new_session.Def.c_device_id.Value = local_device_id;
        new_session.Def.vc_oidc_session_id.Value = session_id;
        await new_session.InsertData(ct);

        return session_id;
    }

    public async Task RemoveAllSessions(ApplicationOption app, CancellationToken ct, Dictionary<string, object>? server_claims = null)
    {
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, server_claims);

        var sessions = new ApplicationOidcActiveSessions(dbc, encryptor);
        await sessions.ExecuteProc(ct, sessions.Def.aos_deleteAllSessions);
    }

    public async Task RemoveSession(ApplicationOption app, string session_id, CancellationToken ct, Dictionary<string, object>? server_claims = null)
    {
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, server_claims);

        ApplicationOidcActiveSessions del_session = new(dbc, encryptor);
        del_session.Def.vc_oidc_session_id.Value = session_id;

        await del_session.ExecuteProc(ct, del_session.Def.aos_deleteSessionSID);
    }

    public async Task RemoveUserSessions(ApplicationOption app, string user_name, CancellationToken ct, Dictionary<string, object>? server_claims = null)
    {
        using var dbc = app.CreateDatabaseClient(log, deviceid_service, server_claims);

        ApplicationOidcActiveSessions del_session = new(dbc, encryptor);
        del_session.Def.vc_username.Value = user_name;
        await del_session.ExecuteProc(ct, del_session.Def.aos_deleteUserSessions);
    }
}
