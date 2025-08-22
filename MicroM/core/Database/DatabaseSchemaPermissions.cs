using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using System.Text;

namespace MicroM.Database;

/// <summary>
/// Grants permissions and routes for database entities.
/// </summary>
public class DatabaseSchemaPermissions
{
    /// <summary>
    /// Creates routing entries for the specified entities.
    /// </summary>
    /// <param name="ec">Entity client.</param>
    /// <param name="entities">Entities to process.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async static Task CreateEntitiesRoutes(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            foreach (var options in entities.Values)
            {
                await options.EntityInstance.CreateEntityRoutes(ec, ct);
            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    /// <summary>
    /// Grants execution rights on all entity procedures to the specified login or group.
    /// </summary>
    /// <param name="ec">Entity client.</param>
    /// <param name="entities">Entities whose procedures will be granted.</param>
    /// <param name="login_or_group">Login or group receiving permissions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async static Task GrantExecutionToAllProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, string login_or_group, CancellationToken ct)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            StringBuilder sb = new();

            foreach (var options in entities.Values)
            {
                sb.AppendLine(options.EntityInstance.AsGrantExecutionToEntityProcsScript(login_or_group));
            }

            await ec.ExecuteSQLNonQuery(sb.ToString(), ct);

        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

    }

    //public static string GrantExecutionToAllProcs<T>(string login_or_group_name) where T : EntityBase, new()
    //{

    //    T entity = new();

    //    return entity.AsGrantExecutionToEntityProcsScript(login_or_group_name);
    //}

}
