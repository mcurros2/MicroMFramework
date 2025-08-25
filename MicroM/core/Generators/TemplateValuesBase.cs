using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Generators
{
    /// <summary>
    /// Provides a base container for storing template tokens.
    /// </summary>
    internal abstract class TemplateValuesBase
    {
        public readonly Dictionary<string, string> tokens = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateValuesBase"/> class and fills the token dictionary.
        /// </summary>
        public TemplateValuesBase()
        {
            FillTokens();
        }

        // MMC: this aims to ease the use of the tokens dictionary
        /// <summary>
        /// Populates the token dictionary with placeholders for init-only string properties.
        /// </summary>
        private void FillTokens()
        {
            object obj = this;

            IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

            // MMC: this is to initialize the tokens dictionary with empty strings for all properties that are init only
            foreach (var prop in instance_members)
            {
                bool isInitOnly = false;

                if (prop.MemberType == MemberTypes.Property && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                {
                    var property = (PropertyInfo)prop;
                    var setMethod = property.SetMethod;
                    if (setMethod != null)
                    {
                        var parameters = setMethod.GetParameters();
                        if (setMethod.ReturnParameter != null)
                        {
                            var requiredModifiers = setMethod.ReturnParameter.GetRequiredCustomModifiers();
                            isInitOnly = requiredModifiers.Any(t => t.FullName == typeof(IsExternalInit).FullName);
                        }
                    }

                    if (isInitOnly && property.PropertyType == typeof(string))
                    {
                        tokens[property.Name] = "";
                    }
                }
            }
        }
    }

}
