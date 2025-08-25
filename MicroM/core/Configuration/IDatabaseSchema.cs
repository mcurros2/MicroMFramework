using MicroM.Core;
using MicroM.Data;
using MicroM.Database;

namespace MicroM.Configuration
{
    /// <summary>
    /// Result values for database migration operations.
    /// </summary>
    public enum DatabaseMigrationResult
    {
        /// <summary>No migration was required.</summary>
        NoMigrationNeeded,
        /// <summary>The schema was migrated successfully.</summary>
        Migrated,
        /// <summary>The schema was not migrated.</summary>
        NotMigrated
    }

    /// <summary>
    /// Provides methods for creating and migrating the MicroM database schema.
    /// </summary>
    public interface IDatabaseSchema
    {
        /// <summary>Get the entity types to build the schema for.</summary>
        /// <param name="ec">Entity client used to retrieve entity definitions.</param>
        /// <param name="ct">Cancellation token for the operation.</param>
        /// <returns>Ordered collection of entity creation options.</returns>
        Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>> GetEntitiesTypes(IEntityClient ec, CancellationToken ct);

        /// <summary>Create or alter schema and stored procedures.</summary>
        /// <param name="ec">Entity client used to execute commands.</param>
        /// <param name="entities">Entity definitions to build.</param>
        /// <param name="ct">Cancellation token for the operation.</param>
        /// <param name="create_or_alter">True to create new or alter existing schema.</param>
        /// <param name="create_if_not_exists">True to create objects only if they do not exist.</param>
        Task CreateDBSchemaAndProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct, bool create_or_alter = true, bool create_if_not_exists = true);

        /// <summary>Grant permissions for the generated schema.</summary>
        /// <param name="ec">Entity client used to execute commands.</param>
        /// <param name="entities">Entities to grant permissions for.</param>
        /// <param name="login_or_group">Login or group receiving permissions.</param>
        /// <param name="ct">Cancellation token for the operation.</param>
        Task GrantPermissions(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, string login_or_group, CancellationToken ct);

        /// <summary>Create menu entries for the generated entities.</summary>
        /// <param name="ec">Entity client used to execute commands.</param>
        /// <param name="entities">Entities whose menu entries will be created.</param>
        /// <param name="ct">Cancellation token for the operation.</param>
        Task CreateMenus(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct);

        /// <summary>Migrate the database to the latest schema version.</summary>
        /// <param name="ec">Entity client used to execute commands.</param>
        /// <param name="ct">Cancellation token for the operation.</param>
        /// <returns>The result of the migration process.</returns>
        Task<DatabaseMigrationResult> MigrateDatabase(IEntityClient ec, CancellationToken ct);
    }
}
