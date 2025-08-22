namespace MicroM.Data
{

    /// <summary>
    /// Provides base functionality for defining foreign key relationships between entities.
    /// </summary>
    public abstract class EntityForeignKeyBase
    {
        private const string STDLookupName = nameof(STDLookupName);

        private string _Name = null!;

        /// <summary>Gets the name of the relationship.</summary>
        public string Name
        {
            get => _Name;
            internal set
            {
                if (string.IsNullOrEmpty(_Name)) _Name = value;
                else throw new ArgumentException($"The property {nameof(Name)} can only be modified if the value is null.");
            }
        }

        /// <summary>The parent entity type in the relationship.</summary>
        public readonly Type ParentEntityType;

        /// <summary>The child entity type in the relationship.</summary>
        public readonly Type ChildEntityType;

        /// <summary>Column mappings that compose the foreign key.</summary>
        public readonly List<BaseColumnMapping> KeyMappings = [];

        /// <summary>Indicates whether the relationship is marked as fake.</summary>
        public readonly bool Fake;

        /// <summary>Indicates whether an index should not be created for the relationship.</summary>
        public readonly bool DoNotCreateIndex;

        /// <summary>Lookups associated with this foreign key.</summary>
        public readonly Dictionary<string, EntityLookup> EntityLookups = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityForeignKeyBase"/> class.
        /// </summary>
        public EntityForeignKeyBase(string name, Type parent_type, Type child_type, bool fake, bool do_not_create_index, List<BaseColumnMapping>? key_mappings)
        {
            Name = name;
            ParentEntityType = parent_type ?? throw new ArgumentNullException(nameof(parent_type));
            ChildEntityType = child_type ?? throw new ArgumentNullException(nameof(child_type));
            Fake = fake;
            DoNotCreateIndex = do_not_create_index;

            if (key_mappings != null) KeyMappings.AddRange(key_mappings);

        }

        /// <summary>
        /// Creates an <see cref="EntityLookup"/> and registers it for this relationship.
        /// </summary>
        public EntityLookup AddLookup(string view, string lookup, int id_index, int description_index, string? key_parameter = null, string compound_key_group = "")
        {
            string lookup_name = $"{view}@{lookup}";
            if (EntityLookups.ContainsKey(lookup_name)) throw new InvalidOperationException($"The lookup {lookup_name} already exists in foreign key {Name}");

            EntityLookup lkp = new(view, lookup, id_index, description_index, key_parameter, compound_key_group);
            EntityLookups.Add(lookup_name, lkp);
            return lkp;
        }



    }
}

