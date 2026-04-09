using MicroM.Core;
using MicroM.Extensions;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Data;

public class ProcedureDefinition
{
    private string _Name = null!;
    public string Name
    {
        get => _Name;
        internal set
        {
            if (string.IsNullOrEmpty(_Name)) _Name = value;
            else throw new ArgumentException($"The property {nameof(Name)} can only be modified if the value is null.");
        }
    }

    private string? _Schema;
    public string? Schema { get; internal set; } = null;

    public string QualifiedName => string.IsNullOrEmpty(_Schema) ? Name : $"[{_Schema}].{Name}";


    public readonly Dictionary<string, ColumnBase> Parms = new(StringComparer.OrdinalIgnoreCase);
    public bool ReadonlyLocks;

    private readonly List<string> _ColumnNames = [];

    public readonly bool isLookup;
    public readonly bool isImport;

    public ProcedureDefinition(string? name = "", bool readonly_locks = false, bool is_lookup = false, bool is_import = false, string[]? from_columns_in_definition = null, ColumnBase[]? parms = null)
    {
        ReadonlyLocks = readonly_locks;
        isLookup = is_lookup;
        isImport = is_import;

        if (isLookup) ReadonlyLocks = true;

        if (from_columns_in_definition != null)
        {
            foreach (string colname in from_columns_in_definition)
            {
                _ColumnNames.Add(colname);
            }
        }

    }

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
    }

    public ProcedureDefinition()
    {
    }

    public ProcedureDefinition(params ColumnBase[] parms) : this(default, default, default, default, default, parms)
    {
    }

    public ProcedureDefinition(params string[] parms) : this(readonly_locks: false, is_lookup: false, is_import: false, parms)
    {
    }

    public ProcedureDefinition(bool readonly_locks = false, params string[] parms) : this(readonly_locks: readonly_locks, is_lookup: false, is_import: false, parms)
    {
    }

    private void FillParmsCollection()
    {
        object obj = this;

        IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

        foreach (var prop in instance_members)
        {
            if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
            {
                if (prop.GetMemberType().IsSubclassOf(typeof(ColumnBase)))
                {
                    var parm = (ColumnBase?)prop.GetMemberValue(obj);
                    if (!Parms.ContainsKey(prop.Name) && parm != null)
                    {
                        parm.Name = prop.Name;
                        Parms.Add(prop.Name, parm);
                    }
                }
            }
        }

    }

    internal void InitializeProc(IReadonlyOrderedDictionary<ColumnBase> cols)
    {
        FillParmsCollection();

        foreach (string colname in _ColumnNames)
        {
            if (!cols.Contains(colname)) throw new ArgumentException($"Procedure {Name}: cannot find a column with name {colname} in the entity definition.");
            AddParmFromCol(cols[colname]!);
        }
        _ColumnNames.Clear();
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
    public virtual Column<T> AddParmFromCol<T>(Column<T> col, T value = default!, bool output = false)
    {
        //string new_name = parm.SQLParameterName;

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


