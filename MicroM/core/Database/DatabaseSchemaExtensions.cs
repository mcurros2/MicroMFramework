using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using System.Reflection;

namespace MicroM.Database
{
    public static class DatabaseSchemaExtensions
    {

        public static void TryAddEntityType<T>(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> dict, IEntityClient ec, bool create_or_alter = true) where T : EntityBase, new()
        {
            var ent = new T();
            ent.Init(ec);
            var type = typeof(T);
            dict.TryAdd(
                type.Name,
                new DatabaseSchemaCreationOptions<EntityBase>(
                    ent,
                    create_or_alter
                )
            );
        }

        public static void TryAddEntities(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> dict, bool create_or_alter = true, params EntityBase[] entities)
        {
            foreach (var entity in entities)
            {
                dict.TryAdd(
                    entity.GetType().Name,
                    new DatabaseSchemaCreationOptions<EntityBase>(
                        entity,
                        create_or_alter
                    )
                );
            }
        }

        public async static Task<CustomOrderedDictionary<CustomScript>> GetAllClassifiedCustomProcs(this Assembly assembly, CancellationToken ct)
        {
            CustomOrderedDictionary<CustomScript> ret = new();

            foreach (string name in assembly.GetManifestResourceNames())
            {
                ct.ThrowIfCancellationRequested();
                if (!name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;

                using var manifest = assembly.GetManifestResourceStream(name);
                if (manifest != null)
                {
                    using StreamReader reader = new(manifest);
                    string custom_sql = await reader.ReadToEndAsync(ct);
                    reader.Close();

                    foreach (var custom_proc in DatabaseSchemaCustomScripts.ClassifyCustomSQLScript(custom_sql))
                    {
                        ret.Add(custom_proc.ProcName == null || !custom_proc.ProcType.IsIn(SQLScriptType.Procedure, SQLScriptType.Function) ? $"{Guid.NewGuid()}" : custom_proc.ProcName, custom_proc);
                    }
                }
            }

            return ret;
        }

        public static CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> GetAllEntities(this Assembly assembly, IEntityClient ec, CancellationToken ct, bool create_or_alter = true)
        {
            CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> ret = new();

            foreach (Type type in assembly.GetTypes())
            {
                ct.ThrowIfCancellationRequested();
                if (typeof(EntityBase).IsAssignableFrom(type))
                {

                    var ent = (EntityBase?)Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Can't create entity instance. {type.Name}");
                    if (ent != null)
                    {
                        ent.Init(ec);
                        ret.Add(type.Name, new DatabaseSchemaCreationOptions<EntityBase>(
                            ent,
                            create_or_alter
                            ));
                    }
                }
            }

            return ret;
        }


    }
}
