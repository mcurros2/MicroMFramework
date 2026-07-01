using MicroM.Core;
using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Data;

/// <summary>
/// This class is used to map a stored procedure that works as a view. The naming convention enforced is [mnemonic]_brw[name of view]
/// ie: "pers_brwStandard"
/// Where "pers" is the mnemonic for Persons entity, "_brw" indicates a view (the acronym cames for browse), "Standard" the name of the view. 
/// </summary>
public class ViewDefinition
{
    /// <summary>
    /// Holds the view parameters <see cref="ViewParm"/>
    /// </summary>
    public readonly Dictionary<string, ViewParm> Parms = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Holds groups for getting Compound Keys. The Key is the name of the group, the value holds the <see cref="ViewParm"/>
    /// </summary>
    public readonly Dictionary<string, List<ViewParm>> CompoundKeyGroups = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates the most significant part of a Key
    /// </summary>
    public ViewParm? BrowsingKeyParm = null!;

    /// <summary>
    /// The procedure that represents the view
    /// </summary>
    public readonly ProcedureDefinition Proc;

    public readonly EntityFilterBase? Filters;

    private void AddViewParms(params ViewParm[] parms)
    {
        foreach (var parm in parms)
        {
            if (parm.Column.Name.IsIn(comparer: StringComparer.OrdinalIgnoreCase, parms: [SystemViewParmNames.like, SystemViewParmNames.d]))
            {
                throw new ArgumentException($"You can´t define parameters for views with system names {SystemViewParmNames.like} or {SystemViewParmNames.d}. Use constructor parameter add_default_parms");
            }
            Parms.Add(parm.Column.Name, parm);
            Proc.AddParm(parm.Column);
            if (parm.BrowsingKey) BrowsingKeyParm = parm;
            CreateCompoundKeyGroups(parm);
        }
    }

    private ViewParm AddViewParmFromCol(ColumnBase col, int column_mapping = -1, bool browsing_key = false)
    {
        ViewParm parm = new(col, column_mapping: column_mapping, browsing_key: browsing_key);
        if (parm.Column.Name.IsIn(comparer: StringComparer.OrdinalIgnoreCase, parms: [SystemViewParmNames.like, SystemViewParmNames.d]))
        {
            throw new ArgumentException($"You can´t define parameters for views with system names {SystemViewParmNames.like} or {SystemViewParmNames.d}. Use constructor parameter add_default_parms");
        }
        Parms.Add(parm.Column.Name, parm);
        Proc.AddParm(parm.Column);
        if (parm.BrowsingKey) BrowsingKeyParm = parm;
        return parm;
    }

    private ViewParm? FillParmsCollection()
    {
        object obj = this;

        IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

        ViewParm? lastParm = null;
        foreach (var prop in instance_members)
        {
            if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
            {
                if (prop.GetMemberType().IsSubclassOf(typeof(ColumnBase)))
                {
                    var parm = (ColumnBase?)prop.GetMemberValue(obj);
                    parm?.Name = prop.Name;
                    if (!Parms.ContainsKey(prop.Name) && parm != null)
                    {
                        if (parm.Name.IsNullOrEmpty()) parm.Name = prop.Name;
                        if (parm.Name != prop.Name) throw new ArgumentException($"The name of the parameter {prop.Name} does not match the property name for procedure {Proc.Name}.");

                        lastParm = AddViewParmFromCol(parm);
                    }
                }
            }
        }

        return lastParm;
    }

    internal bool IsInitialized = false;
    internal void CreateParmsFromNames(IReadonlyOrderedDictionary<ColumnBase> cols)
    {
        if (IsInitialized) return;

        ViewParm? lastParm = null;
        lastParm = FillParmsCollection();

        for (int x = 0; x < _ColumnNames.Count; x++)
        {
            string colname = _ColumnNames[x];
            if (!cols.Contains(colname)) throw new ArgumentException($"View {Proc.Name}: cannot find a column with name {colname} in the entity definition.");
            lastParm = AddViewParmFromCol(cols[colname]!);
        }

        lastParm?.BrowsingKey = true;
        lastParm?.ColumnMapping = 0;
        BrowsingKeyParm = lastParm;

        _ColumnNames.Clear();
        AddDefaultParms();
        IsInitialized = true;
    }

    private readonly List<string> _ColumnNames = [];

    public ViewDefinition(bool add_default_parms = true, params string[] parms)
    {
        Proc = new ProcedureDefinition(readonly_locks: true);
        foreach (var col in parms)
        {
            _ColumnNames.Add(col);
        }
        IsInitialized = false;
    }

    /// <summary>
    /// StandardView constructor indicating the defined column names. When the entity definition is created, the view parameters are created.
    /// </summary>
    /// <param name="parms"></param>
    public ViewDefinition(params string[] parms) : this(add_default_parms: true, parms)
    {
    }


    public ViewDefinition(string? name = "", bool add_default_parms = true, params ViewParm[] parms)
    {
        Proc = new ProcedureDefinition(name: name, readonly_locks: true);
        AddViewParms(parms);
        if (add_default_parms) AddDefaultParms();
    }

    public ViewDefinition(bool add_default_parms = true, params ViewParm[] parms) : this(default, add_default_parms, parms)
    {
    }

    /// <summary>
    /// Constructs a view with a filters entity. The view will have the default parameters <b>like</b> and <b>d</b>
    /// all the columns in the filters entity will be added as parameters.
    /// </summary>
    /// <param name="filters_entity"></param>
    /// <param name="parms"></param>
    public ViewDefinition(EntityFilterBase filters_entity, params string[] parms) : this(add_default_parms: true, parms)
    {
        Filters = filters_entity;
        // Get the column names from columns properties in the filters entity
        foreach (var col_name in filters_entity.FilterEntityType.GetColumnNames())
        {
            if (!col_name.IsIn(SystemColumnNames.AsStringArray) && col_name != nameof(EntityDefinition.AutonumColumn) && !_ColumnNames.Contains(col_name)) _ColumnNames.Add(col_name);
        }
    }

    private void CreateCompoundKeyGroups(ViewParm parm)
    {

        if (!string.IsNullOrEmpty(parm.CompoundGroup))
        {
            if (!CompoundKeyGroups.TryGetValue(parm.CompoundGroup, out List<ViewParm>? value))
            {
                CompoundKeyGroups.Add(parm.CompoundGroup, [parm]);
            }
            else
            {
                value.Add(parm);
            }

        }

    }

    /// <summary>
    /// Add the default parameters <b>like</b> that will receive a search string.
    /// </summary>
    private bool _defaultParmsAdded = false;
    private void AddDefaultParms()
    {
        if (_defaultParmsAdded) return;

        Column<string> d = Column<string>.Char(name: SystemViewParmNames.d, size: 1);
        Column<string[]> like = Column<string[]>.Text(name: SystemViewParmNames.like, size: 0, isArray: true);

        Parms.Add(like.Name, new ViewParm(like));
        Proc.AddParm(like);

        Parms.Add(d.Name, new ViewParm(d));
        Proc.AddParm(d);

        _defaultParmsAdded |= true;
    }

    public static EntityFilter<T> CreateFilters<T>(string name = "") where T : EntityBase
    {
        return new EntityFilter<T>(name);
    }

}
