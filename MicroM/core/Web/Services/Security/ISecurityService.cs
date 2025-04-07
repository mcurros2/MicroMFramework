namespace MicroM.Web.Services.Security
{

    public interface ISecurityService
    {
        public bool IsAuthorized(string app_id, string route_path, Dictionary<string, object?> server_claims);

        public Task RefreshGroupsSecurityRecords(string? app_id, CancellationToken ct);

    }
}
