using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using System.Security.Claims;

namespace MicroM.Extensions
{
    /// <summary>
    /// Helper methods for converting claims and generating route paths used by
    /// the security subsystem.
    /// </summary>
    public static class SecurityExtensions
    {
        /// <summary>
        /// Converts a dictionary of claim types and values into a sequence of
        /// <see cref="Claim"/> objects.
        /// </summary>
        /// <param name="dictionary">Dictionary containing claim types and their values.</param>
        /// <returns>An enumerable of claims created from the dictionary.</returns>
        public static IEnumerable<Claim> ToClaims(this IDictionary<string, object> dictionary)
        {
            return dictionary.Select(kv => new Claim(kv.Key, kv.Value?.ToString() ?? string.Empty));
        }

        /// <summary>
        /// Builds the list of route paths for the specified entity type based
        /// on the provided <paramref name="route_flags"/>. Optional arrays can
        /// further restrict views, procedures or actions to include.
        /// </summary>
        /// <param name="entity_type">Type of the entity.</param>
        /// <param name="route_flags">Flags that describe which routes to generate.</param>
        /// <param name="views">Optional list of view names to include.</param>
        /// <param name="procs">Optional list of procedure names to include.</param>
        /// <param name="actions">Optional list of action names to include.</param>
        /// <returns>A list of route paths matching the specified flags.</returns>
        public static List<string> GetRoutePaths(this Type entity_type, AllowedRouteFlags route_flags, string[]? views = null, string[]? procs = null, string[]? actions = null)
        {
            ArgumentNullException.ThrowIfNull(entity_type);
            if (!typeof(EntityBase).IsAssignableFrom(entity_type))
            {
                throw new ArgumentException($"The type {entity_type.Name} is not an EntityBase type.");
            }

            if (!route_flags.HasFlag(AllowedRouteFlags.Views) && (views?.Length > 0))
            {
                route_flags |= AllowedRouteFlags.Views;
            }

            if (!route_flags.HasFlag(AllowedRouteFlags.Procs) && (procs?.Length > 0))
            {
                route_flags |= AllowedRouteFlags.Procs;
            }

            if (!route_flags.HasFlag(AllowedRouteFlags.Actions) && (actions?.Length > 0))
            {
                route_flags |= AllowedRouteFlags.Actions;
            }

            var entity = (EntityBase?)Activator.CreateInstance(entity_type);
            if (entity == null) return [];

            return entity.GetRoutePaths(entity_type.Name, route_flags, views, procs, actions);
        }

        /// <summary>
        /// Builds the list of route paths for the given entity instance.
        /// </summary>
        /// <param name="entity">Entity instance providing metadata.</param>
        /// <param name="entity_name">Name of the entity.</param>
        /// <param name="route_flags">Flags describing which routes to include.</param>
        /// <param name="views">Optional view names.</param>
        /// <param name="procs">Optional procedure names.</param>
        /// <param name="actions">Optional action names.</param>
        /// <returns>List of generated route paths.</returns>
        public static List<string> GetRoutePaths(this EntityBase entity, string entity_name, AllowedRouteFlags route_flags, string[]? views = null, string[]? procs = null, string[]? actions = null)
        {
            List<string> paths = [];

            if (route_flags.HasFlag(AllowedRouteFlags.Get))
            {
                paths.Add($"{entity_name}/get");
            }

            if (route_flags.HasFlag(AllowedRouteFlags.Insert))
            {
                paths.Add($"{entity_name}/insert");
            }

            if (route_flags.HasFlag(AllowedRouteFlags.Update))
            {
                paths.Add($"{entity_name}/update");
            }

            if (route_flags.HasFlag(AllowedRouteFlags.Delete))
            {
                paths.Add($"{entity_name}/delete");
            }

            if (route_flags.HasFlag(AllowedRouteFlags.DefaultLookup))
            {
                paths.Add($"{entity_name}/lookup");
            }

            if (route_flags.HasFlag(AllowedRouteFlags.Import))
            {
                paths.Add($"{entity_name}/import");
            }

            if (route_flags.HasFlag(AllowedRouteFlags.Views))
            {
                foreach (var view in entity.Def.Views.Values)
                {
                    if ((views != null && views.Contains(view.Proc.Name)) || route_flags == AllowedRouteFlags.All)
                    {
                        paths.Add($"{entity_name}/view/{view.Proc.Name}");
                    }
                }
            }

            if (route_flags.HasFlag(AllowedRouteFlags.Procs))
            {
                foreach (var proc in entity.Def.Procs.Values)
                {
                    if ((procs != null && procs.Contains(proc.Name)) || route_flags == AllowedRouteFlags.All)
                    {
                        if (proc.isLookup)
                        {
                            paths.Add($"{entity_name}/lookup/{proc.Name}");
                        }
                        else
                        {
                            paths.Add($"{entity_name}/proc/{proc.Name}");
                            paths.Add($"{entity_name}/process/{proc.Name}");
                        }
                    }
                }
            }


            if (route_flags.HasFlag(AllowedRouteFlags.Actions))
            {
                foreach (var act in entity.Def.Actions)
                {
                    if ((actions != null && actions.Contains(act.Key)) || route_flags == AllowedRouteFlags.All)
                    {
                        paths.Add($"{entity_name}/action/{act.Key}");
                    }
                }
            }

            return paths;

        }

        /// <summary>
        /// Persists the routes generated for the specified entity into the
        /// database using the provided client.
        /// </summary>
        /// <param name="entity">Entity whose routes will be created.</param>
        /// <param name="ec">Client used to interact with the database.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes when all routes have been stored.</returns>
        public async static Task CreateEntityRoutes(this EntityBase entity, IEntityClient ec, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entity);
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);

            try
            {
                var entity_type = entity.GetType();
                var paths = entity.GetRoutePaths(entity_type.Name, AllowedRouteFlags.All);

                var routes = new MicromRoutes(ec);

                foreach (var path in paths)
                {
                    routes.Def.c_route_id.Value = "";
                    routes.Def.vc_route_path.Value = path;
                    await routes.InsertData(ct);
                }

            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Creates all route paths for the specified entity type and persists
        /// them using the provided database client.
        /// </summary>
        /// <param name="entity_type">Entity type for which to create routes.</param>
        /// <param name="ec">Database client used to store the routes.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async static Task CreateEntityRoutes(this Type entity_type, IEntityClient ec, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entity_type);
            if (!typeof(EntityBase).IsAssignableFrom(entity_type))
            {
                throw new ArgumentException($"The type {entity_type.Name} is not an EntityBase type.");
            }

            EntityBase? entity = (EntityBase?)Activator.CreateInstance(entity_type);
            if (entity == null) return;

            await entity.CreateEntityRoutes(ec, ct);

        }

    }
}
