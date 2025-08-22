namespace MicroM.Data
{
    public class EntityUniqueConstraint
    {
        public readonly string[] Keys;

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

        public EntityUniqueConstraint(string name = "", params ColumnBase[] keys)
        {
            if (keys.Length == 0) throw new ArgumentException("You must provide at least one key column to create a unique constraint");
            Keys = keys.Select<ColumnBase, string>(col => col.Name).ToArray();
            Name = name;
        }

        public EntityUniqueConstraint(string name = "", params string[] keys)
        {
            if (keys.Length == 0) throw new ArgumentException("You must provide at least one key column to create a unique constraint");
            Keys = keys;
            Name = name;
        }


    }
}
