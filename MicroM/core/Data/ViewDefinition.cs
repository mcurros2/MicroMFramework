using MicroM.Core;
using MicroM.Extensions;
using System.Data;
using System.Globalization;

namespace MicroM.Data
{
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
        public ViewParm BrowsingKeyParm = null!;

        /// <summary>
        /// The procedure that represents the view
        /// </summary>
        public readonly ProcedureDefinition Proc;

        public readonly EntityFilterBase? Filters;

        private void AddViewParms(params ViewParm[] parms)
        {
            foreach (var parm in parms)
            {
                if (parm.Column.Name.ToLower(CultureInfo.InvariantCulture).IsIn(SystemViewParmNames.like, SystemViewParmNames.d))
                {
                    throw new ArgumentException($"You can´t define parameters for views with system names {SystemViewParmNames.like} or {SystemViewParmNames.d}. Use constructor parameter add_default_parms");
                }
                Parms.Add(parm.Column.Name, parm);
                Proc.AddParm(parm.Column);
                if (parm.BrowsingKey) BrowsingKeyParm = parm;
                CreateCompoundKeyGroups(parm);
            }
        }

        private void AddViewParmFromCol(ColumnBase col, int column_mapping = -1, bool browsing_key = false)
        {
            ViewParm parm = new(col, column_mapping: column_mapping, browsing_key: browsing_key);
            if (parm.Column.Name.ToLowerInvariant().IsIn(SystemViewParmNames.like, SystemViewParmNames.d))
            {
                throw new ArgumentException($"You can´t define parameters for views with system names {SystemViewParmNames.like} or {SystemViewParmNames.d}. Use constructor parameter add_default_parms");
            }
            Parms.Add(parm.Column.Name, parm);
            Proc.AddParm(parm.Column);
            if (parm.BrowsingKey) BrowsingKeyParm = parm;
        }

        internal bool IsInitialized = true;
        internal void CreateParmsFromNames(IReadonlyOrderedDictionary<ColumnBase> cols)
        {
            if (_ColumnNames.Count == 0 || IsInitialized) return;

            int last_column = _ColumnNames.Count - 1;

            for (int x = 0; x < _ColumnNames.Count; x++)
            {
                string colname = _ColumnNames[x];
                if (!cols.Contains(colname)) throw new ArgumentException($"View {Proc.Name}: cannot find a column with name {colname} in the entity definition.");
                if (x != last_column)
                {
                    AddViewParmFromCol(cols[colname]!);
                }
                else
                {
                    AddViewParmFromCol(cols[colname]!, 0, true);
                }

            }
            _ColumnNames.Clear();
            AddDefaultParms();
            IsInitialized = true;
        }

        private readonly List<string> _ColumnNames = [];

        public ViewDefinition(bool add_default_parms = true, params string[] parms)
        {
            Proc = new ProcedureDefinition("", true);
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
            Proc = new ProcedureDefinition(name, true);
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
                if(!col_name.IsIn(SystemColumnNames.AsStringArray) && col_name != nameof(EntityDefinition.AutonumColumn) && !_ColumnNames.Contains(col_name)) _ColumnNames.Add(col_name);
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
        /// <exception cref="InvalidOperationException"></exception>
        private void AddDefaultParms()
        {
            if (Proc.Parms.ContainsKey(SystemViewParmNames.like) || Proc.Parms.ContainsKey(SystemViewParmNames.d))
            {
                throw new InvalidOperationException($"The default columns for view {Proc.Name} had already been added.");
            }

            Column<string[]> like;
            Column<string> d;

            like = new(SystemViewParmNames.like, sql_type: SqlDbType.VarChar, size: 0, isArray: true);
            Parms.Add(like.Name, new ViewParm(like));
            Proc.AddParm(like);

            d = new(SystemViewParmNames.d, sql_type: SqlDbType.Char, size: 1);
            Parms.Add(d.Name, new ViewParm(d));
            Proc.AddParm(d);
        }

        public static EntityFilter<T> CreateFilters<T>(string name = "") where T : EntityBase
        {
            return new EntityFilter<T>(name);
        }

    }
}
