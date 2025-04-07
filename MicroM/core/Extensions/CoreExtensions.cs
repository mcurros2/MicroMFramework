using MicroM.Core;
using MicroM.Data;
using MicroM.Generators.SQLGenerator;

namespace MicroM.Extensions
{
    public static class CoreExtensions
    {
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

        public static EntityForeignKey<TParent, TChild> AddFK<TParent, TChild>(this Dictionary<string, EntityForeignKeyBase> collection, string name, bool fake = false, List<BaseColumnMapping>? key_mappings = null) where TParent : EntityBase where TChild : EntityBase
        {
            var fk = new EntityForeignKey<TParent, TChild>(name, fake, key_mappings);
            collection.Add(name, fk);
            return fk;
        }


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

        public static void SetColumnValue(this EntityBase entity, string col_name, Dictionary<string, object> values)
        {
            if (entity.Def.Columns.TryGetValue(col_name, out ColumnBase? col))
            {
                if (col != null) col.ValueObject = values[col_name];
            }
        }

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

        public static string ToTableName<T>()
        {
            return typeof(T).Name.ToSQLName();
        }

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

        public static string IfNullOrEmpty(this string value, string null_or_empty_value)
        {
            return string.IsNullOrEmpty(value) ? null_or_empty_value : value;
        }

        public static string ThrowIfNullOrEmpty(this string value, string? parm_name)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(parm_name);
            return value;
        }

        public static (bool allowed, string extension) IsFileExtensionAllowed(this string fileName, string[] allowedExtensions)
        {
            var fileExtension = Path.GetExtension(fileName);
            return (allowed: fileExtension.IsIn(allowedExtensions), extension: fileExtension);
        }
    }
}
