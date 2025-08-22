namespace MicroM.Web.Services.Security
{

    /// <summary>
    /// Represents the ISecurityService.
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Performs the IsAuthorized operation.
        /// </summary>
        public bool IsAuthorized(string app_id, string route_path, Dictionary<string, object?> server_claims);

        /// <summary>
        /// Performs the RefreshGroupsSecurityRecords operation.
        /// </summary>
        public Task RefreshGroupsSecurityRecords(string? app_id, CancellationToken ct);

    }
}
