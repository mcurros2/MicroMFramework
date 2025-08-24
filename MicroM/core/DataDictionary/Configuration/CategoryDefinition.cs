using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.DataDictionary.Configuration
{
    /// <summary>
    /// Base class used to describe a category and its possible values.
    /// </summary>
    public abstract class CategoryDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryDefinition"/> class.
        /// </summary>
        public CategoryDefinition() { }

        /// <summary>
        /// Gets the unique identifier of the category. Defaults to the class name.
        /// </summary>
        public string CategoryID { get; protected set; } = "";

        /// <summary>
        /// Gets a human readable description for the category.
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        /// Gets a value indicating whether multiple values can be assigned to the category.
        /// </summary>
        public bool Multivalue { get; init; } = false;

        /// <summary>
        /// Gets the collection of possible values for this category keyed by value identifier.
        /// </summary>
        public Dictionary<string, CategoryValuesDefinition> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryDefinition"/> class with a description and multivalue option.
        /// </summary>
        /// <param name="description">Text describing the category.</param>
        /// <param name="multivalue">True if the category accepts multiple values.</param>
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
