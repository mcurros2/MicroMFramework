namespace MicroM.Web.Services.Security
{

    public interface ISecurityService
    {
        public bool IsAuthorized(string app_id, string route_path, Dictionary<string, object?> server_claims);

        public void InvalidateGroupsSecurityRecords(string app_id);

        public Task RefreshGroupsSecurityRecords(string? app_id, CancellationToken ct);

    }
}
