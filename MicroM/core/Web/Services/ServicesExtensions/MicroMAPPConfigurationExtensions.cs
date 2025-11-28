using MicroM.Configuration;
using MicroM.Data;
using MicroM.DataDictionary.CategoriesDefinitions;
using MicroM.Extensions;
using MicroM.Web.Authentication;
using Microsoft.Extensions.Logging;

namespace MicroM.Web.Services;

public static class MicroMAPPConfigurationExtensions
{
    public static DatabaseClient CreateDatabaseClient(this ApplicationOption app, ILogger log, IDeviceIdService? deviceIdService, Dictionary<string, object>? server_claims, int connection_timeout_secs = 5)
    {
        string user = app.AuthenticationType == nameof(AuthenticationTypes.SQLServerAuthentication) && string.IsNullOrEmpty(app.SQLUser) ? (string?)server_claims?[MicroMServerClaimTypes.MicroMUsername] ?? "" : app.SQLUser;
        string pass = app.AuthenticationType == nameof(AuthenticationTypes.SQLServerAuthentication) && string.IsNullOrEmpty(app.SQLUser) ? (string?)server_claims?[MicroMServerClaimTypes.MicroMPassword] ?? "" : app.SQLPassword;

        string local_device_id = "";
        if (server_claims != null)
        {
            server_claims.TryGetValue(MicroMServerClaimTypes.MicroMUserDeviceID, out var local_device_claim);

            local_device_id = local_device_claim?.ToString() ?? "";
        }

        string workstation_id = "";
        if (deviceIdService != null)
        {
            var (device_id, ipaddress, user_agent) = deviceIdService.GetDeviceID(local_device_id);
            workstation_id = $"{ipaddress} {device_id} {user_agent}".Truncate(128);
        }

        DatabaseClient dbc = new(
            server: app.SQLServer,
            user: user,
            password: pass,
            db: app.SQLDB,
            logger: log,
            connection_timeout_secs: connection_timeout_secs,
            server_claims: server_claims)
        {
            // TODO: add to server configuration option pooling options
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 500,
            ApplicationName = $"{app.ApplicationName}",
            WorkstationID = workstation_id,
        };
        return dbc;
    }

}
