namespace MicroM.Data
{

    public abstract class EntityForeignKeyBase
    {
        private const string STDLookupName = nameof(STDLookupName);

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

        public readonly Type ParentEntityType;
        public readonly Type ChildEntityType;
        public readonly List<BaseColumnMapping> KeyMappings = [];
        public readonly bool Fake;
        public readonly bool DoNotCreateIndex;

        public readonly Dictionary<string, EntityLookup> EntityLookups = new(StringComparer.OrdinalIgnoreCase);

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
        /// This will create an <see cref="EntityLookup"/> and add it to the lookups for this relationship
        /// </summary>
        /// <returns></returns>
        public EntityLookup AddLookup(string view, string lookup, int id_index, int description_index, string? key_parameter = null, string compound_key_group = "")
        {
            string lookup_name = $"{view}@{lookup}";
            if (EntityLookups.ContainsKey(lookup_name)) throw new InvalidOperationException($"The lookup {lookup_name} already exits in foreign key {Name}");

            EntityLookup lkp = new(view, lookup, id_index, description_index, key_parameter, compound_key_group);
            EntityLookups.Add(lookup_name, lkp);
            return lkp;
        }



    }
}
