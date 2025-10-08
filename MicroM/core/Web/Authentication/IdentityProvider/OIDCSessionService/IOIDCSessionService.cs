using MicroM.Configuration;

namespace MicroM.Web.Authentication.SSO;

public interface IOIDCSessionService
{
    Task<string> CreateSession(ApplicationOption app, string client_app_id, string user_name, CancellationToken ct, string local_device_id = "", Dictionary<string, object>? server_claims = null);

    Task RemoveSession(ApplicationOption app, string session_id, CancellationToken ct, Dictionary<string, object>? server_claims = null);

    Task RemoveUserSessions(ApplicationOption app, string user_name, CancellationToken ct, Dictionary<string, object>? server_claims = null);

    Task RemoveAllSessions(ApplicationOption app, CancellationToken ct, Dictionary<string, object>? server_claims = null);

}
