
namespace MicroM.Data
{
    /// <summary>
    /// Provides the base functionality for entity filters.
    /// </summary>
    public abstract class EntityFilterBase
    {
        private string _name = null!;

        /// <summary>
        /// Gets the name of the filter.
        /// </summary>
        public string Name
        {
            get => _name;
            internal set
            {
                if (string.IsNullOrEmpty(_name)) _name = value;
                else throw new ArgumentException($"The property {nameof(Name)} can only be modified if the value is null.");
            }
        }

        /// <summary>
        /// Gets the entity type associated with this filter.
        /// </summary>
        public readonly Type FilterEntityType;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFilterBase"/> class.
        /// </summary>
        /// <param name="name">Filter name.</param>
        /// <param name="filter_entity_type">Type of the filtered entity.</param>
        public EntityFilterBase(string name, Type filter_entity_type)
        {
            Name = name;
            FilterEntityType = filter_entity_type;
        }
    }
}
