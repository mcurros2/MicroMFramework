﻿namespace MicroM.Web.Services.Security
{

    /// <summary>
    /// Exposes authorization operations for MicroM applications. Implementations
    /// verify requests by comparing user group memberships against in-memory
    /// route caches that are populated from a configuration store.
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Determines whether a request is authorized for a specific application
        /// route. Checks globally allowed routes and group-based permissions
        /// previously loaded by <see cref="RefreshGroupsSecurityRecords"/>.
        /// </summary>
        /// <param name="app_id">Unique identifier of the application receiving the request.</param>
        /// <param name="route_path">Relative path of the route being accessed.</param>
        /// <param name="server_claims">Claims supplied by the server describing the current user.</param>
        /// <returns><see langword="true"/> when access is granted; otherwise <see langword="false"/>.</returns>
        public bool IsAuthorized(string app_id, string route_path, Dictionary<string, object?> server_claims);

        /// <summary>
        /// Rehydrates the cached security records for all groups belonging to the
        /// specified application by reading the latest data from the configuration
        /// store.
        /// </summary>
        /// <param name="app_id">Application identifier whose security records are refreshed.</param>
        /// <param name="ct">Token that cancels the refresh operation.</param>
        /// <returns>A task representing the asynchronous refresh process.</returns>
        public Task RefreshGroupsSecurityRecords(string? app_id, CancellationToken ct);

    }
}

