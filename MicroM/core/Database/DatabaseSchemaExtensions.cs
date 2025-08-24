using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using System.Reflection;

namespace MicroM.Database
{
    /// <summary>
    /// Extension methods for building database schemas from entities and assemblies.
    /// </summary>
    public static class DatabaseSchemaExtensions
    {

        /// <summary>
        /// Adds the specified entity type to the dictionary if it does not exist.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="dict">Dictionary of creation options.</param>
        /// <param name="ec">Entity client.</param>
        /// <param name="create_or_alter">Indicates if objects should be created or altered.</param>
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

        /// <summary>
        /// Adds the provided entity instances to the dictionary if they do not exist.
        /// </summary>
        /// <param name="dict">Dictionary of creation options.</param>
        /// <param name="create_or_alter">Indicates if objects should be created or altered.</param>
        /// <param name="entities">Entities to add.</param>
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

        /// <summary>
        /// Retrieves and classifies all embedded SQL scripts in the assembly.
        /// </summary>
        /// <param name="assembly">Assembly to scan.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Dictionary of classified custom scripts.</returns>
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

        /// <summary>
        /// Creates a dictionary of all entity types contained in the assembly.
        /// </summary>
        /// <param name="assembly">Assembly to scan.</param>
        /// <param name="ec">Entity client.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="create_or_alter">Indicates if objects should be created or altered.</param>
        /// <returns>Dictionary of entity creation options.</returns>
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
