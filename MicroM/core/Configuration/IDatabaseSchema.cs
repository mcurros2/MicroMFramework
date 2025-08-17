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
        NoMigrationNeeded,
        Migrated,
        NotMigrated
    }

    /// <summary>
    /// Provides methods for creating and migrating the MicroM database schema.
    /// </summary>
    public interface IDatabaseSchema
    {
        /// <summary>Get the entity types to build the schema for.</summary>
        Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>> GetEntitiesTypes(IEntityClient ec, CancellationToken ct);

        /// <summary>Create or alter schema and stored procedures.</summary>
        Task CreateDBSchemaAndProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct, bool create_or_alter = true, bool create_if_not_exists = true);

        /// <summary>Grant permissions for the generated schema.</summary>
        Task GrantPermissions(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, string login_or_group, CancellationToken ct);

        /// <summary>Create menu entries for the generated entities.</summary>
        Task CreateMenus(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct);

        /// <summary>Migrate the database to the latest schema version.</summary>
        Task<DatabaseMigrationResult> MigrateDatabase(IEntityClient ec, CancellationToken ct);
    }
}
