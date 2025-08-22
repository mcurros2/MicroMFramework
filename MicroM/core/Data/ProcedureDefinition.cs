using MicroM.Core;
using System.Data;

namespace MicroM.Data
{
    /// <summary>
    /// Describes a stored procedure and its parameters for entity operations.
    /// </summary>
    public class ProcedureDefinition
    {
        private string _Name = null!;

        /// <summary>Gets the name of the stored procedure.</summary>
        public string Name
        {
            get => _Name;
            internal set
            {
                if (string.IsNullOrEmpty(_Name)) _Name = value;
                else throw new ArgumentException($"The property {nameof(Name)} can only be modified if the value is null.");
            }
        }

        /// <summary>Parameters defined for the procedure.</summary>
        public readonly Dictionary<string, ColumnBase> Parms = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Indicates whether the procedure acquires read-only locks.</summary>
        public bool ReadonlyLocks;

        private readonly List<string> _ColumnNames = [];

        public readonly bool isLookup;
        public readonly bool isImport;

        private bool IsInitialized = true;
        public ProcedureDefinition(bool readonly_locks = false, bool is_lookup = false, bool is_import = false, params string[] parms)
        {
            ReadonlyLocks = readonly_locks;

            isLookup = is_lookup;
            if (isLookup) ReadonlyLocks = true;

            isImport = is_import;

            foreach (var col in parms)
            {
                _ColumnNames.Add(col);
            }
            IsInitialized = false;
        }

        public ProcedureDefinition(string? name = "", bool readonly_locks = false, bool is_lookup = false, params ColumnBase[] parms)
        {
            Name = name!;
            ReadonlyLocks = readonly_locks;

            isLookup = is_lookup;
            if (isLookup) ReadonlyLocks = true;

            AddParmsFromCols(parms);
        }

        public ProcedureDefinition()
        {
        }

        public ProcedureDefinition(params ColumnBase[] parms) : this(default, default, default, parms)
        {
        }

        public ProcedureDefinition(params string[] parms) : this(readonly_locks: false, is_lookup: false, is_import: false, parms)
        {
        }

        public ProcedureDefinition(bool readonly_locks = false, params string[] parms) : this(readonly_locks: readonly_locks, is_lookup: false, is_import: false, parms)
        {
        }

        internal void CreateParmsFromNames(IReadonlyOrderedDictionary<ColumnBase> cols)
        {
            if (_ColumnNames.Count == 0 || IsInitialized) return;
            foreach (string colname in _ColumnNames)
            {
                if (!cols.Contains(colname)) throw new ArgumentException($"Procedure {Name}: cannot find a column with name {colname} in the entity definition.");
                AddParmFromCol(cols[colname]!);
            }
            _ColumnNames.Clear();
            IsInitialized = true;
        }

        public void AddParmsFromCols(params ColumnBase[] parms)
        {
            foreach (var col in parms)
            {
                AddParmFromCol(col);
            }
        }


        /// <summary>
        /// Takes a <see cref="ColumnBase"/> as a template and creates a <see cref="Column{T}"/> and adds it to the <see cref="Parms"/> collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="col">The Column template</param>
        /// <param name="strip_prefix">Will strip the prefix of a column, which parameters don't use. Ie. c_category_id will become category_id</param>
        /// <param name="value">The value for the parameter</param>
        /// <param name="output">Defines the SP parameter as OUTPUT</param>
        /// <returns></returns>
        public virtual Column<T> AddParmFromCol<T>(Column<T> col, T value = default!, bool output = false)
        {
            //string new_name = col.SQLParameterName;

            Column<T> parm = new(col, output: output) { Value = value };

            Parms.Add(parm.Name, parm);
            return parm;
        }

        public ColumnBase AddParmFromCol(ColumnBase col, bool output = false)
        {
            Type SQLColType = typeof(Column<>).MakeGenericType(col.SystemType);
            ColumnBase parm = (ColumnBase?)Activator.CreateInstance(SQLColType, col, col.Name, output) ?? throw new ArgumentException($"Unable to create a {SQLColType.Name} from {col.SystemType.Name}.");

            var property = typeof(ColumnBase).GetProperty(nameof(ColumnBase.OverrideWith));
            property?.SetValue(parm, col.OverrideWith);

            Parms.Add(parm.Name, parm);
            return parm;
        }

        /// <summary>
        /// Internal use for adding parameters when creating a view
        /// </summary>
        /// <param name="new_parm"></param>
        internal void AddParm(ColumnBase new_parm) { Parms[new_parm.Name] = new_parm; }

        /// <summary>
        /// Creates a <see cref="Column{T}"/> and adds it to the <see cref="Parms"/> collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name for the parameter</param>
        /// <param name="sql_type">The SqlDbType for this parameter</param>
        /// <param name="size">The size for the parameer</param>
        /// <param name="precision">Precision for numeric parameters</param>
        /// <param name="scale">Scale for numeric parameters</param>
        /// <param name="value">The value for the parameter</param>
        /// <param name="output">Defines the SP parameter as OUTPUT</param>
        /// <returns></returns>
        public Column<T> AddParm<T>(string name, SqlDbType? sql_type, int size = 0, byte precision = 0, byte scale = 0, T value = default!, bool output = false, string? override_with = null)
        {
            Column<T> parm = new(name, value, sql_type, size, precision, scale, output, override_with: override_with);
            Parms.Add(parm.Name, parm);
            return parm;

        }

        public ColumnBase this[string name]
        {
            get
            {
                return Parms[name];
            }
        }


    }

}


