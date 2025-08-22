using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.DataDictionary.Configuration
{
    public abstract class CategoryDefinition
    {
        public CategoryDefinition() { }

        public string CategoryID { get; protected set; } = "";
        public string Description { get; init; } = "";

        public bool Multivalue { get; init; } = false;

        public Dictionary<string, CategoryValuesDefinition> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public CategoryDefinition(string description, bool multivalue = false)
        {
            CategoryID = this.GetType().Name;
            Description = description;
            Multivalue = multivalue;

            FillValuesDictionary();
        }

        private void FillValuesDictionary()
        {
            IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

            foreach (var prop in instance_members)
            {
                if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                {
                    if (prop.GetMemberType() == typeof(CategoryValuesDefinition))
                    {
                        var category_value = (CategoryValuesDefinition?)prop.GetMemberValue(this);
                        if (category_value != null)
                        {
                            if (Values.TryAdd(prop.Name, category_value))
                            {
                                category_value.CategoryValueID = prop.Name;
                            }
                            else
                            {
                                throw new ArgumentException($"Duplicate Category Value: Value {category_value.CategoryValueID} ({category_value.Description}), Category {this.CategoryID} ({Description})");
                            }
                        }
                    }
                }
            }
        }

    }

}
