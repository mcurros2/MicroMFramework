using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using MicroM.Web.Authentication;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Core
{
    /// <summary>
    /// Defines the structure of an Entity.
    /// </summary>
    public abstract class EntityDefinition
    {
        /// <summary>
        /// Mnemonic code representing this Entity. This string will be used as a prefix for every stored procedure that belongs to this Entity.
        /// Standard SP names will be expected in the form of MNEO_update, MNEO_get, etc.
        /// </summary>
        public string Mneo { get; protected set; }

        private string _ParentClassName = null!;

        /// <summary>
        /// Returns the name of the class
        /// </summary>
        public string Name => _ParentClassName;

        private string _TableName = null!;
        /// <summary>
        /// The name of the table in SQL Server
        /// </summary>
        public string TableName
        {
            get => _TableName;
            init
            {
                _ParentClassName = value;
                _TableName = value?.ToSQLName() ?? throw new ArgumentNullException(nameof(TableName));
            }
        }

        /// <summary>
        /// Indicates that this entity is fake. Fake entities don´t have tables in SQL Server. They can have stored procedures and there own mnemonic code.
        /// </summary>
        public bool Fake { get; init; } = false;

        /// <summary>
        /// Options used when generating SQL for this entity.
        /// </summary>
        public SQLCreationOptionsMetadata SQLCreationOptions = SQLCreationOptionsMetadata.None;

        private readonly CustomOrderedDictionary<ColumnBase> _Columns = new();
        /// <summary>
        /// Dictionary of Columns for this Entity
        /// </summary>
        public IReadonlyOrderedDictionary<ColumnBase> Columns => _Columns;

        /// <summary>
        /// The column that represents the most significant Key column for this entity. The value of this column will be set when executing a lookup.
        /// Example: CategoriesValues entity key = c_category_id, c_value_id. KeyColumnName = c_value_id. c_category_id is expected as a parent key.
        /// </summary>
        public string KeyColumnName { get; protected set; } = null!;

        private readonly Dictionary<string, ViewDefinition> _Views = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Dictionary with defined views for this entity. Views are expected to not update data and will enforce a no locking transaction isolation level.
        /// This will be reflected when executing the StandardView to enforce CQRS. Views in MicroM are stored procedures not SQL views.
        /// </summary>
        public IReadOnlyDictionary<string, ViewDefinition> Views => _Views;

        private readonly Dictionary<string, ProcedureDefinition> _Procs = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Dictionary with defined stored procedures for this entity. Procs are expected to update data and will not change transaction isolation level.
        /// This will be reflected when executing the SP to enforce CQRS.
        /// </summary>
        public IReadOnlyDictionary<string, ProcedureDefinition> Procs => _Procs;

        private readonly Dictionary<string, EntityForeignKeyBase> _ForeignKeys = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Dictionary with defined foreign keys for this entity.
        /// </summary>
        public IReadOnlyDictionary<string, EntityForeignKeyBase> ForeignKeys => _ForeignKeys;

        private readonly Dictionary<string, EntityActionBase> _actions = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Dictionary with defined actions for this entity.
        /// </summary>
        public IReadOnlyDictionary<string, EntityActionBase> Actions => _actions;



        /// <summary>
        /// Returns the <see cref="EntityForeignKeyBase"/> that relates to <see cref="EntityForeignKeyBase.ParentEntityType"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public EntityForeignKeyBase? GetForeignKey<T>(T parent_entity) where T : EntityBase
        {
            foreach (var fk in _ForeignKeys)
            {
                if (fk.Value.ParentEntityType == parent_entity.GetType())
                {
                    return (EntityForeignKeyBase)fk.Value;
                }
            }
            return null;
        }

        private readonly Dictionary<string, EntityUniqueConstraint> _UniqueConstraints = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Dictionary with defined unique constraints for this entity.
        /// </summary>
        public IReadOnlyDictionary<string, EntityUniqueConstraint> UniqueConstraints => _UniqueConstraints;


        private readonly Dictionary<string, EntityIndex> _Indexes = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Dictionary with defined indexes for this entity.
        /// </summary>
        public IReadOnlyDictionary<string, EntityIndex> Indexes => _Indexes;

        #region "Default properties"

        private ColumnBase _autonumcol = null!;
        /// <summary>
        /// Returns the column wih bit flag <see cref="ColumnFlags.Autonum"/>. There can only be one autonum column in the definition.
        /// </summary>
        public ColumnBase AutonumColumn => _autonumcol;

        /// <summary>
        /// This default column stores the date and time when the record was created
        /// </summary>
        public Column<DateTime> dt_inserttime { get; private set; } = null!;
        /// <summary>
        /// This default column stores the date and time when the record was last updated. It is used to detect concurrency issues while updating data.
        /// </summary>
        public Column<DateTime> dt_lu { get; private set; } = null!;
        /// <summary>
        /// This default column stores the web user authenticated that created the record. Usually is the email of the authenticated user.
        /// </summary>
        public Column<string> vc_webinsuser { get; private set; } = null!;
        /// <summary>
        /// This default column stores the web user authenticated that last updated the record. Usually is the email of the authenticated user.
        /// </summary>
        public Column<string> vc_webluuser { get; private set; } = null!;
        /// <summary>
        /// This default column stores the AD user authenticated that created the record.
        /// </summary>
        public Column<string> vc_insuser { get; private set; } = null!;
        /// <summary>
        /// This default column stores the AD user authenticated that last updated the record.
        /// </summary>
        public Column<string> vc_luuser { get; private set; } = null!;
        /// <summary>
        /// This default column stores the web user authenticated that created the record. Usually is the email of the authenticated user.
        /// </summary>
        public Column<string> webusr { get; private set; } = null!;

        #endregion

        #region "Categories and Status"

        readonly HashSet<string> _RelatedCategories = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Returns the list of related category IDs for this entity
        /// </summary>
        public IReadOnlySet<string> RelatedCategories => _RelatedCategories;

        /// <summary>
        /// Relates a category ID with the entity
        /// </summary>
        /// <param name="category_id"></param>
        public void AddCategoryID(string category_id)
        {
            _RelatedCategories.Add(category_id);
        }

        readonly HashSet<string> _RelatedStatus = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Returns the list of related status IDs for this entity
        /// </summary>
        public IReadOnlySet<string> RelatedStatus => _RelatedStatus;

        /// <summary>
        /// Relates a status ID with the entity
        /// </summary>
        /// <param name="status_id"></param>
        public void AddStatusID(string status_id)
        {
            _RelatedStatus.Add(status_id);
        }

        #endregion

        #region "Column methods"

        private bool _DefaultColumnsAdded = false;
        private void AddDefaultColumns(bool webuser_delete_flag = false)
        {
            if (_DefaultColumnsAdded) return;

            //var def_cols = _Columns.AddDefaultColumns();
            var def_cols = new DefaultColumns();

            dt_inserttime = def_cols.dt_inserttime;
            dt_lu = def_cols.dt_lu;
            vc_webinsuser = def_cols.vc_webinsuser;
            vc_webluuser = def_cols.vc_webluuser;
            vc_insuser = def_cols.vc_insuser;
            vc_luuser = def_cols.vc_luuser;
            webusr = !webuser_delete_flag ? def_cols.webusr : Column<string>.Text(column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Fake | ColumnFlags.Delete, override_with: nameof(MicroMServerClaimTypes.MicroMUsername), value: ""); ;

            _DefaultColumnsAdded = true;
        }

        #endregion

        /// <summary>
        /// It is expected that you implement a parameter-less constructor that calls this one with the definition mnemonic code: base("mnemonic").
        /// This constructor will call DefineProcs(), DefineViews(), DefineForeignKeys() in this order.
        /// </summary>
        /// <param name="mneo">Sets the mnemonic code used to create all SPs that belong to this entity</param>
        /// <param name="name">Sets the name for this entity. Use nameof(YourEntityName). This will rsult in the table name in the DB</param>
        /// <param name="add_default_columns">If true it will add the default columns to the entity</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected EntityDefinition(string mneo, string name, bool add_default_columns = true, bool webusr_delete_flag = false)
        {
            Mneo = mneo ?? throw new ArgumentNullException(nameof(mneo));
            TableName = name ?? throw new ArgumentNullException(nameof(name));

            if (add_default_columns) AddDefaultColumns(webusr_delete_flag);

            FillColumnsCollectionAndColumnNames(); // MMC: set the names of columns first as they can be used in other definitions

            DefineProcs();
            DefineViews();
            DefineConstraints();
            DefineActions();

            FillCollectionsAndSetPropertyNames(); // MMC: set the rest of properties collections and names

            AddCategoriesRelations();
            AddStatusRelations();
        }

        /// <summary>
        /// Add Actions. It will be called from the base constructor.
        /// </summary>
        protected virtual void DefineActions() { }

        /// <summary>
        /// Add a relation with a CategoryID. It will be called from the base constructor.
        /// </summary>
        protected virtual void AddCategoriesRelations() { }
        /// <summary>
        /// Add a relation with a StatusID. It will be called from the base constructor.
        /// </summary>
        protected virtual void AddStatusRelations() { }
        /// <summary>
        /// Define Views inside this function. It will be called from the base constructor.
        /// </summary>
        protected virtual void DefineViews() { }
        /// <summary>
        /// Define Procedures inside this function. It will be called from the base constructor.
        /// </summary>
        protected virtual void DefineProcs() { }
        /// <summary>
        /// Define Foreign Keys and Unique constraints for this entity.
        /// </summary>
        protected virtual void DefineConstraints() { }

        private static readonly ConcurrentDictionary<string, byte> _ValidatedDefinitions = new();
        internal void ValidateDefinition(string definition_class_name)
        {
            if (!_ValidatedDefinitions.ContainsKey(definition_class_name))
            {
                foreach (var col in _Columns.Values)
                {
                    if (col.ColumnMetadata.HasFlag(ColumnFlags.Autonum))
                    {
                        if (!string.Equals(_autonumcol.Name, col.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"Definition class {definition_class_name} - {col.Name} can´t be defined as autonum because {_autonumcol.Name} is already defined. The entity definition has more than one column defined as {nameof(ColumnFlags.Autonum)}, which is not allowed.");
                        }
                    }

                }

                foreach (var proc in _Procs.Values)
                {
                    if (!proc.Name.StartsWith($"{this.Mneo}_", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Definition for class {definition_class_name} - {proc.Name}: invalid name definition. The entity definition must have all proc names starting with the entity mnemonic code '{Mneo}_'.");
                    }
                }

                foreach (var view in _Views.Values)
                {
                    if (!view.Proc.Name.StartsWith($"{this.Mneo}_", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Definition for class {definition_class_name} - {view.Proc.Name}: invalid name definition. The entity definition must have all proc names starting with the entity mnemonic code '{Mneo}_'.");
                    }
                }

                foreach (var fk in _ForeignKeys.Values)
                {
                    string child_table = fk.ChildEntityType.Name;
                    if (!string.Equals(child_table.ToSQLName(), TableName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Definition for class {_ParentClassName} - {fk.Name}: invalid foreign key definition. The child entity {child_table} is not allowed. Only {_ParentClassName} is allowed as a child table.");
                    }
                }
                _ValidatedDefinitions.TryAdd(definition_class_name, 0);
            }
        }

        private void FillColumnsCollectionAndColumnNames()
        {
            object obj = this;

            IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

            foreach (var prop in instance_members)
            {
                if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                {
                    if (prop.GetMemberType().IsSubclassOf(typeof(ColumnBase)))
                    {
                        var col = (ColumnBase?)prop.GetMemberValue(obj);
                        if (!_Columns.Contains(prop.Name) && col != null)
                        {
                            col.Name = prop.Name;
                            if (col.ColumnMetadata.HasFlag(ColumnFlags.Autonum)) _autonumcol = col;
                            _Columns.Add(prop.Name, col);
                            if (!string.IsNullOrEmpty(col.RelatedCategoryID)) AddCategoryID(col.RelatedCategoryID);
                            if (!string.IsNullOrEmpty(col.RelatedStatusID)) AddStatusID(col.RelatedStatusID);
                        }
                    }
                }
            }

        }

        private void FillCollectionsAndSetPropertyNames()
        {
            object obj = this;

            IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

            foreach (var prop in instance_members)
            {
                if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                {
                    if (prop.GetMemberType() == typeof(ProcedureDefinition))
                    {
                        var proc = (ProcedureDefinition?)prop.GetMemberValue(obj);
                        if (!_Procs.ContainsKey(prop.Name) && proc != null)
                        {
                            proc.Name = prop.Name;
                            proc.CreateParmsFromNames(Columns);
                            _Procs.Add(prop.Name, proc);
                        }
                    }
                    else if (prop.GetMemberType() == typeof(ViewDefinition))
                    {
                        var view = (ViewDefinition?)prop.GetMemberValue(obj);
                        if (!_Views.ContainsKey(prop.Name) && view != null)
                        {
                            var combinedColumns = GetColumnsAndFilters(view);

                            view.Proc.Name = prop.Name;
                            view.CreateParmsFromNames(combinedColumns);
                            _Views.Add(prop.Name, view);
                        }
                    }
                    else if (prop.GetMemberType().IsSubclassOf(typeof(EntityForeignKeyBase)))
                    {
                        var fk = (EntityForeignKeyBase?)prop.GetMemberValue(obj);
                        if (!_ForeignKeys.ContainsKey(prop.Name) && fk != null)
                        {
                            fk.Name = $"{Mneo}_{prop.Name}";
                            _ForeignKeys.Add(fk.Name, fk);
                        }
                    }
                    else if (prop.GetMemberType().IsAssignableTo(typeof(EntityUniqueConstraint)))
                    {
                        var un = (EntityUniqueConstraint?)prop.GetMemberValue(obj);
                        if (!_UniqueConstraints.ContainsKey(prop.Name) && un != null)
                        {
                            un.Name = $"{Mneo}_{prop.Name}";
                            _UniqueConstraints.Add(prop.Name, un);
                        }
                    }
                    else if (prop.GetMemberType().IsAssignableTo(typeof(EntityIndex)))
                    {
                        var idx = (EntityIndex?)prop.GetMemberValue(obj);
                        if (!_Indexes.ContainsKey(prop.Name) && idx != null)
                        {
                            idx.Name = $"{Mneo}_{prop.Name}";
                            _Indexes.Add(prop.Name, idx);
                        }
                    }
                    else if (prop.GetMemberType().IsSubclassOf(typeof(EntityActionBase)))
                    {
                        var act = (EntityActionBase?)prop.GetMemberValue(obj);
                        if (!_actions.ContainsKey(prop.Name) && act != null)
                        {
                            _actions.Add(prop.Name, act);
                        }
                    }

                }
            }

        }

        // Filters handling
        private static CustomOrderedDictionary<ColumnBase> GetFilterColumns(ViewDefinition viewDefinition)
        {
            var filterEntityType = viewDefinition.Filters?.FilterEntityType;
            if (filterEntityType != null)
            {
                EntityBase? filterEntityInstance = Activator.CreateInstance(filterEntityType) as EntityBase;
                var filterDefinitionProperty = filterEntityType.GetProperties(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(p => p.Name == nameof(EntityBase.Def));
                if (filterEntityInstance != null && filterDefinitionProperty != null)
                {
                    EntityDefinition? filterDefinition = filterDefinitionProperty.GetValue(filterEntityInstance) as EntityDefinition;
                    if (filterDefinition != null)
                    {
                        return filterDefinition._Columns;
                    }
                }
            }
            return new CustomOrderedDictionary<ColumnBase>();
        }

        private IReadonlyOrderedDictionary<ColumnBase> GetColumnsAndFilters(ViewDefinition viewDefinition)
        {
            var filterColumns = GetFilterColumns(viewDefinition);
            if (filterColumns.Count == 0)
            {
                return Columns;
            }
            var combinedColumns = new CustomOrderedDictionary<ColumnBase>();
            foreach (var column in Columns)
            {
                combinedColumns.Add(column.Name, column);
            }

            foreach (var filterColumn in filterColumns)
            {
                combinedColumns.TryAdd(filterColumn.Name, filterColumn);
            }

            return combinedColumns;
        }


    }
}
