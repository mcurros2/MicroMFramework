namespace MicroM.Web.Services.Security
{

    /// <summary>
    /// Defines operations for verifying route permissions and maintaining
    /// the in-memory security cache for MicroM applications.
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Determines whether a request is authorized based on the configured
        /// security rules and the claims provided by the server.
        /// </summary>
        /// <param name="app_id">Identifier of the application receiving the request.</param>
        /// <param name="route_path">The route path being accessed.</param>
        /// <param name="server_claims">Claims extracted from the authenticated user.</param>
        /// <returns>
        /// <see langword="true"/> when the request should be allowed; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool IsAuthorized(string app_id, string route_path, Dictionary<string, object?> server_claims);

        /// <summary>
        /// Refreshes the cached security records for all groups belonging to
        /// the specified application. The records are retrieved from the
        /// underlying configuration store.
        /// </summary>
        /// <param name="app_id">Application identifier whose security records are refreshed.</param>
        /// <param name="ct">Token used to cancel the refresh operation.</param>
        /// <returns>A task that completes when the refresh process ends.</returns>
        public Task RefreshGroupsSecurityRecords(string? app_id, CancellationToken ct);

    }
}

