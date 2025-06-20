﻿using MicroM.Data;

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
        public Task CreateDBSchemaAndProcs(IEntityClient ec, CancellationToken ct, bool create_or_alter = true, bool create_if_not_exists = true);

        public Task GrantPermissions(IEntityClient ec, string login_or_group, CancellationToken ct);

        public Task CreateMenus(IEntityClient ec, CancellationToken ct);
        
        public Task<DatabaseMigrationResult> MigrateDatabase(IEntityClient ec, CancellationToken ct);

    }
}
