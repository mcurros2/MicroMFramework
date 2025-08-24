using MicroM.Core;
using MicroM.Data;
using MicroM.Generators.SQLGenerator;

namespace MicroM.Extensions
{
    public static class CoreExtensions
    {
        /// <summary>
        /// Adds a new procedure definition to the collection and registers its parameters.
        /// </summary>
        /// <param name="collection">Procedure collection.</param>
        /// <param name="name">Procedure name.</param>
        /// <param name="readonly_locks">Indicates if the procedure uses read-only locks.</param>
        /// <param name="parms">Parameters to add.</param>
        /// <returns>The created procedure definition.</returns>
        public static ProcedureDefinition AddProc(this Dictionary<string, ProcedureDefinition> collection, string name, bool readonly_locks = false, params ColumnBase[] parms)
        {
            var proc = new ProcedureDefinition(name, readonly_locks);
            foreach (var col in parms)
            {
                proc.AddParmFromCol(col);
            }
            collection.Add(proc.Name, proc);
            return proc;
        }

        /// <summary>
        /// Registers a foreign key definition in the collection.
        /// </summary>
        /// <typeparam name="TParent">Parent entity type.</typeparam>
        /// <typeparam name="TChild">Child entity type.</typeparam>
        /// <param name="collection">Foreign key collection.</param>
        /// <param name="name">Foreign key name.</param>
        /// <param name="fake">Indicates a fake foreign key.</param>
        /// <param name="do_not_create_index">Prevents index creation when true.</param>
        /// <param name="key_mappings">Optional column mappings.</param>
        /// <returns>The created foreign key definition.</returns>
        public static EntityForeignKey<TParent, TChild> AddFK<TParent, TChild>(this Dictionary<string, EntityForeignKeyBase> collection, string name, bool fake = false, bool do_not_create_index = false, List<BaseColumnMapping>? key_mappings = null) where TParent : EntityBase where TChild : EntityBase
        {
            var fk = new EntityForeignKey<TParent, TChild>(name, fake, do_not_create_index, key_mappings);
            collection.Add(name, fk);
            return fk;
        }


        /// <summary>
        /// Adds a new view definition to the collection and optionally adds default parameters.
        /// </summary>
        /// <param name="collection">View collection.</param>
        /// <param name="name">View name.</param>
        /// <param name="add_default_parms">Whether to add default parameters.</param>
        /// <param name="parms_columns">Optional columns used for parameters.</param>
        /// <returns>The created view definition.</returns>
        public static ViewDefinition AddView(this Dictionary<string, ViewDefinition> collection, string name, bool add_default_parms = true, IReadonlyOrderedDictionary<ColumnBase>? parms_columns = null)
        {
            ViewDefinition view = new(name, add_default_parms);
            collection.Add(view.Proc.Name, view);

            if (parms_columns != null)
            {
                foreach (var col in parms_columns.GetWithFlags(ColumnFlags.PK))
                {
                    view.Proc.AddParmFromCol(col);
                }
            }

            return view;
        }

        /// <summary>
        /// Sets key column values on the entity from the provided dictionary.
        /// </summary>
        /// <param name="entity">Entity instance.</param>
        /// <param name="values">Values keyed by column name.</param>
        public static void SetKeyValues(this EntityBase entity, Dictionary<string, object> values)
        {
            foreach (string key in values.Keys)
            {
                if (entity.Def.Columns.TryGetValue(key, out ColumnBase? col))
                {
                    if (col != null && col.ColumnMetadata.HasAnyFlag(ColumnFlags.FK | ColumnFlags.PK))
                    {
                        col.ValueObject = values[key];
                    }
                }
            }
        }

        /// <summary>
        /// Assigns column values on an entity using a dictionary of values.
        /// </summary>
        /// <param name="entity">Entity instance.</param>
        /// <param name="values">Values keyed by column name.</param>
        public static void SetColumnValues(this EntityBase entity, Dictionary<string, object> values)
        {
            foreach (string key in values.Keys)
            {
                if (entity.Def.Columns.TryGetValue(key, out ColumnBase? col))
                {
                    if (col != null) col.ValueObject = values[key];
                }
            }
        }

        /// <summary>
        /// Sets a single column value on an entity.
        /// </summary>
        /// <param name="entity">Entity instance.</param>
        /// <param name="col_name">Column name.</param>
        /// <param name="values">Dictionary containing the value.</param>
        public static void SetColumnValue(this EntityBase entity, string col_name, Dictionary<string, object> values)
        {
            if (entity.Def.Columns.TryGetValue(col_name, out ColumnBase? col))
            {
                if (col != null) col.ValueObject = values[col_name];
            }
        }

        /// <summary>
        /// Defines a procedure for the entity definition using the specified columns.
        /// </summary>
        /// <typeparam name="T">Entity definition type.</typeparam>
        /// <param name="def">Entity definition instance.</param>
        /// <param name="readonly_locks">Indicates if the procedure uses read-only locks.</param>
        /// <param name="column_names">Names of columns to include as parameters.</param>
        /// <returns>The created procedure definition.</returns>
        public static ProcedureDefinition DefineProc<T>(this T def, bool readonly_locks = false, params string[] column_names) where T : EntityDefinition
        {
            if (column_names.Length == 0)
            {
                return new ProcedureDefinition("", readonly_locks);
            }

            ColumnBase[] parms = new ColumnBase[column_names.Length];

            int x = 0;
            foreach (string colname in column_names)
            {
                parms[x] = def.Columns[colname] ?? throw new ArgumentException($"Cannot find the column with the name {colname}");
            }

            return new ProcedureDefinition("", readonly_locks, default, parms);
        }

        /// <summary>
        /// Returns the SQL table name for the specified entity type.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <returns>The SQL table name.</returns>
        public static string ToTableName<T>()
        {
            return typeof(T).Name.ToSQLName();
        }

        /// <summary>
        /// Creates a copy of the entity optionally cloning its connection.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="original">Original entity.</param>
        /// <param name="clone_connection">Whether to clone the entity's connection.</param>
        /// <returns>A new entity instance with copied values.</returns>
        public static T Clone<T>(this T original, bool clone_connection) where T : EntityBase, new()
        {
            IEntityClient ec = clone_connection ? original.Client.Clone() : original.Client;
            T result = new();
            result.Init(ec);
            result.CopyFrom(original);
            return result;
        }

        /// <summary>
        /// Copy column values from source if names matches
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="source"></param>
        public static void CopyFrom(this EntityBase entity, EntityBase source)
        {
            if (entity == null) return;
            entity.Def.Columns.CopyColumnValuesByName(source.Def.Columns);
        }

        /// <summary>
        /// Checks whether a file extension is among the allowed extensions.
        /// </summary>
        /// <param name="fileName">File name to inspect.</param>
        /// <param name="allowedExtensions">Array of allowed extensions.</param>
        /// <returns>Tuple indicating if the extension is allowed and the extension value.</returns>
        public static (bool allowed, string extension) IsFileExtensionAllowed(this string fileName, string[] allowedExtensions)
        {
            var fileExtension = Path.GetExtension(fileName);
            return (allowed: fileExtension.IsIn(allowedExtensions), extension: fileExtension);
        }
    }
}
