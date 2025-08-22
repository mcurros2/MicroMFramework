using MicroM.Core;
using MicroM.Data;
using MicroM.Database;

namespace MicroM.Configuration
{
    public enum DatabaseMigrationResult
    {
        NoMigrationNeeded,
        Migrated,
        NotMigrated
    }

    public interface IDatabaseSchema
    {
        public Task<CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>>> GetEntitiesTypes(IEntityClient ec, CancellationToken ct);

        public Task CreateDBSchemaAndProcs(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct, bool create_or_alter = true, bool create_if_not_exists = true);

        public Task GrantPermissions(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, string login_or_group, CancellationToken ct);

        public Task CreateMenus(IEntityClient ec, CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, CancellationToken ct);

        public Task<DatabaseMigrationResult> MigrateDatabase(IEntityClient ec, CancellationToken ct);

    }
}
