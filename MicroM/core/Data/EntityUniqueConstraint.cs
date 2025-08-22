namespace MicroM.Data
{
    /// <summary>
    /// Represents a unique constraint composed of one or more columns.
    /// </summary>
    public class EntityUniqueConstraint
    {
        /// <summary>The column names that form the unique constraint.</summary>
        public readonly string[] Keys;

        private string _name = null!;

        /// <summary>Gets the name of the constraint.</summary>
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
        /// Initializes a new instance using <see cref="ColumnBase"/> definitions.
        /// </summary>
        public EntityUniqueConstraint(string name = "", params ColumnBase[] keys)
        {
            if (keys.Length == 0) throw new ArgumentException("You must provide at least one key column to create a unique constraint");
            Keys = keys.Select<ColumnBase, string>(col => col.Name).ToArray();
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance using raw column names.
        /// </summary>
        public EntityUniqueConstraint(string name = "", params string[] keys)
        {
            if (keys.Length == 0) throw new ArgumentException("You must provide at least one key column to create a unique constraint");
            Keys = keys;
            Name = name;
        }


    }
}

