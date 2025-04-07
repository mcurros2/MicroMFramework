
namespace MicroM.Data
{
    public abstract class EntityFilterBase
    {
        private string _name = null!;
        public string Name
        {
            get => _name;
            internal set
            {
                if (string.IsNullOrEmpty(_name)) _name = value;
                else throw new ArgumentException($"The property {nameof(Name)} can only be modified if the value is null.");
            }
        }

        public readonly Type FilterEntityType;

        public EntityFilterBase(string name, Type filter_entity_type)
        {
            Name = name;
            FilterEntityType = filter_entity_type;
        }
    }
}
