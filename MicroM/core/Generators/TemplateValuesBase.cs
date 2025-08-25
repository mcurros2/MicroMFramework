using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Generators
{
    /// <summary>
    /// Base class that manages placeholder tokens for template replacement.
    /// </summary>
    internal abstract class TemplateValuesBase
    {
        /// <summary>
        /// Stores token–value pairs for template processing.
        /// </summary>
        public readonly Dictionary<string, string> tokens = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateValuesBase"/> class and fills the token dictionary.
        /// </summary>
        public TemplateValuesBase()
        {
            FillTokens();
        }

        /// <summary>
        /// Prepopulates the <see cref="tokens"/> dictionary with placeholders.
        /// </summary>
        /// <remarks>
        /// Adds an empty string entry for each init-only string property defined on the instance, simplifying
        /// subsequent token replacement.
        /// </remarks>
        private void FillTokens()
        {
            object obj = this;

            IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

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
