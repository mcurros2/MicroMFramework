namespace MicroM.Data
{
    /// <summary>
    /// Defines how to look up for a record in a view for an Entity. Each view parameter can be mapped to a column for the returned view, indication the column number that corresponds to it´s value.
    /// 
    /// <see cref="EntityLookup"/> is part of <see cref="EntityForeignKey{TParent, TChild}"/>. For each relationship you can define multiple
    /// lookups.
    /// </summary>
    public sealed class EntityLookup
    {
        /// <summary>
        /// The view which is to be used to lookup for a record in the parent entity defined at <see cref="EntityForeignKey{TParent, TChild}"/>
        /// </summary>
        public string ViewName { get; private set; }
        /// <summary>
        /// The stored procedure to use to perform a lookup and get the description for the primary key of the parent entity defined at <see cref="EntityForeignKey{TParent, TChild}"/>
        /// </summary>
        public string LookupProcName { get; private set; }
        /// <summary>
        /// Indicates the key that will be used to perform a lookup for a record for an entity with more than one column defined as primary key.
        /// The other keys are assumed to be fixed values for a parent level. Use null if there is only one column for the primary key.
        /// </summary>
        public string? KeyParameter { get; private set; }
        /// <summary>
        /// Specify the compound key group name for the view in <see cref="ViewDefinition"/> to get the values of the compound key. 
        /// An empty string will assume there is only one compound key group for the view.
        /// </summary>
        public string CompoundKeyGroup { get; private set; }

        /// <summary>
        /// Indicates the column that will be used as description column from the view. Used to build lookupSelects
        /// </summary>
        public int DescriptionColumnIndex { get; private set; }

        /// <summary>
        /// Indicates the column that will be used as the most significant ID column from the view.
        /// </summary>
        public int IDColumnIndex { get; private set; }

        /// <summary>
        /// Creates a lookup.
        /// </summary>
        /// <param name="view">The <see cref="ViewDefinition"/> to perform the lookup</param>
        /// <param name="lookup">The <see cref="ProcedureDefinition"/> to execute the lookup for a given record</param>
        /// <param name="id_index">The column index tithin the results of the sepcified view that will be used as the most significant ID column for the view.</param>
        /// <param name="description_index">The column index for the results of the specified view, that will be used as Description</param>
        /// <param name="key_parameter">The <see cref="ColumnBase"/> for the parameter key mapping. Only for Entities with more than one column in the primary key. </param>
        /// <param name="compound_key_group">If the <see cref="ViewDefinition"/> has more than one <see cref="ViewDefinition.CompoundKeyGroups"/> the parameter value indicates which one to use</param>
        /// <exception cref="ArgumentException"></exception>
        public EntityLookup(string view, string lookup, int id_index, int description_index, string? key_parameter = null, string compound_key_group = "")
        {

            ViewName = view ?? throw new ArgumentNullException(nameof(view));
            LookupProcName = lookup ?? throw new ArgumentNullException(nameof(lookup));
            KeyParameter = key_parameter;
            CompoundKeyGroup = compound_key_group;
            IDColumnIndex = id_index;
            DescriptionColumnIndex = description_index;
        }
    }
}
