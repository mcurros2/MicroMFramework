using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.DataDictionary.Configuration;
using System.Reflection;
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

            string? grant_statement = (string?)genericMethod.Invoke(null, new object[] { login_or_group });

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



    }
}
