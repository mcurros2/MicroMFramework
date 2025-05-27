using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Configuration;
using MicroM.Generators.Extensions;
using MicroM.Generators.SQLGenerator;
using System.Reflection;
using System.Text;
using static MicroM.Database.DatabaseManagement;

namespace MicroM.Extensions
{
    public static class DatabaseSchemaExtensions
    {
        public static async Task CreateAssemblyCustomProcs(this Assembly assembly, IEntityClient ec, CancellationToken ct, string? mneo = null, string? starts_with = null)
        {
            foreach (string script in await assembly.GetAssemblyCustomProcs(mneo, starts_with, ct))
            {
                try
                {
                    await ec.ExecuteSQLNonQuery(script, ct);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error executing script: {script}: {ex.Message}", ex);
                }
            }
        }

        public async static Task CreateAllCategories(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var categories = asm.GetCategoriesTypes();
            try
            {
                await ec.Connect(ct);
                foreach (var category_type in categories.Values)
                {
                    CategoryDefinition? cat = (CategoryDefinition?)Activator.CreateInstance(category_type);
                    if (cat != null) await cat.AddCategory(ec, ct);
                }
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public async static Task CreateAllStatus(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var status = asm.GetStatusTypes();
            try
            {
                await ec.Connect(ct);
                foreach (var status_type in status.Values)
                {
                    StatusDefinition? stat = (StatusDefinition?)Activator.CreateInstance(status_type);
                    if (stat != null) await stat.AddStatus(ec, ct);
                }
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public static async Task GrantExecutionToEntityProcsByType(Type entityType, string login_or_group, IEntityClient ec, CancellationToken ct)
        {
            if (!typeof(EntityBase).IsAssignableFrom(entityType) || entityType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException("The provided type is not an Entity");
            }

            MethodInfo? method = typeof(DatabaseManagement).GetMethod(nameof(GrantExecutionToAllProcs), BindingFlags.Public | BindingFlags.Static);

            MethodInfo genericMethod = (method?.MakeGenericMethod(entityType)) ?? throw new ArgumentException("The provided type is not an Entity");

            string? grant_statement = (string?)genericMethod.Invoke(null, [login_or_group]);

            if (!string.IsNullOrEmpty(grant_statement)) await ec.ExecuteSQLNonQuery(grant_statement, ct);
        }

        public async static Task GrantExecutionToAllDBProcs(this Assembly asm, string login_or_group, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);

                foreach (var entity_type in entities.Values)
                {
                    await GrantExecutionToEntityProcsByType(entity_type, login_or_group, ec, ct);
                }

            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }

        }
        public async static Task CreateAllRoutes(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);

                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        await entity_type.CreateEntityRoutes(ec, ct);
                    }
                }
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public async static Task CreateAllDatabaseSchemaAndDictionary(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);

                StringBuilder sb_drop_FKs = new();
                StringBuilder sb_drop_PKs = new();
                StringBuilder sb_drop_UNs = new();
                StringBuilder sb_drop_IDXs = new();

                StringBuilder sb_create_tables = new();
                StringBuilder sb_create_UNs = new();
                StringBuilder sb_create_FKs = new();
                StringBuilder sb_create_IDXs = new();
                StringBuilder sb_create_PROCS = new();

                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);


                            sb_drop_PKs.Append(ent.AsDropPrimaryKey());
                            sb_drop_FKs.Append(ent.AsDropForeignKeys());
                            sb_drop_UNs.Append(ent.AsDropUniqueConstraints());
                            sb_drop_IDXs.Append(ent.AsDropIndexes());

                            sb_create_tables.Append(ent.AsCreateTable(table_and_primary_key_only: true));
                            sb_create_UNs.Append(ent.AsAlterUniqueConstraints());
                            sb_create_FKs.Append(ent.AsAlterForeignKeys());
                            sb_create_IDXs.Append(ent.AsCreateIndexes());

                            sb_create_PROCS.AppendLine(string.Join("\n", ent.AsCreateUpdateProc(create_or_alter: true)));
                            sb_create_PROCS.AppendLine(ent.AsCreateGetProc(create_or_alter: true));
                            sb_create_PROCS.AppendLine(string.Join("\n", ent.AsCreateDropProc(create_or_alter: true)));
                            sb_create_PROCS.AppendLine(ent.AsCreateLookupProc(create_or_alter: true));
                            sb_create_PROCS.AppendLine(ent.AsCreateViewProc(create_or_alter: true));

                        }
                    }
                }

                // drop constraints and indexes
                await ec.ExecuteSQLNonQuery(sb_drop_IDXs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_drop_FKs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_drop_UNs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_drop_PKs.ToString(), ct);

                // create
                await ec.ExecuteSQLNonQuery(sb_create_tables.ToString().RemoveEmptyLines(), ct);
                await ec.ExecuteSQLNonQuery(sb_create_UNs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_create_FKs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_create_IDXs.ToString(), ct);

                await ec.ExecuteSQLNonQuery(sb_create_PROCS.ToString(), ct);

                // add to data dictionary
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            await ent.AddInstanceToDataDictionary(ct);
                        }
                    }
                }

            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public static async Task CreateAllTables(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_create_tables = new();
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            sb_create_tables.Append(ent.AsCreateTable(table_and_primary_key_only: true));
                        }
                    }
                }
                // create tables
                await ec.ExecuteSQLNonQuery(sb_create_tables.ToString(), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public static async Task DropAllConstraintsAndIndexes(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_drop_FKs = new();
                StringBuilder sb_drop_PKs = new();
                StringBuilder sb_drop_UNs = new();
                StringBuilder sb_drop_IDXs = new();
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            sb_drop_PKs.Append(ent.AsDropPrimaryKey());
                            sb_drop_FKs.Append(ent.AsDropForeignKeys());
                            sb_drop_UNs.Append(ent.AsDropUniqueConstraints());
                            sb_drop_IDXs.Append(ent.AsDropIndexes());
                        }
                    }
                }
                // drop constraints and indexes
                await ec.ExecuteSQLNonQuery(sb_drop_IDXs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_drop_FKs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_drop_UNs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_drop_PKs.ToString(), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public static async Task CreateAllConstraintsAndIndexes(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);

                StringBuilder sb_create_PKs = new StringBuilder();    
                StringBuilder sb_create_UNs = new();
                StringBuilder sb_create_FKs = new();
                StringBuilder sb_create_IDXs = new();
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            sb_create_PKs.Append(ent.AsAlterPrimaryKey());
                            sb_create_UNs.Append(ent.AsAlterUniqueConstraints());
                            sb_create_FKs.Append(ent.AsAlterForeignKeys());
                            sb_create_IDXs.Append(ent.AsCreateIndexes());
                        }
                    }
                }
                // create constraints and indexes
                await ec.ExecuteSQLNonQuery(sb_create_PKs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_create_UNs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_create_FKs.ToString(), ct);
                await ec.ExecuteSQLNonQuery(sb_create_IDXs.ToString(), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public static async Task AddAllEntitiesToDataDictionary(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            await ent.AddInstanceToDataDictionary(ct);
                        }
                    }
                }
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public static async Task DropAllIndexes(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_drop_IDXs = new();
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            sb_drop_IDXs.Append(ent.AsDropIndexes());
                        }
                    }
                }
                // drop indexes
                await ec.ExecuteSQLNonQuery(sb_drop_IDXs.ToString(), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

        public static async Task CreateAllIndexes(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var entities = asm.GetEntitiesTypes();
            try
            {
                await ec.Connect(ct);
                StringBuilder sb_create_IDXs = new();
                foreach (var entity_type in entities.Values)
                {
                    if (entity_type != null)
                    {
                        var ent = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new InvalidOperationException($"Can't create entity instance. {entity_type.Name}");
                        if (ent != null)
                        {
                            ent.Init(ec);
                            sb_create_IDXs.Append(ent.AsCreateIndexes());
                        }
                    }
                }
                // create indexes
                await ec.ExecuteSQLNonQuery(sb_create_IDXs.ToString(), ct);
            }
            finally
            {
                if (should_close) await ec.Disconnect();
            }
        }

    }
}
