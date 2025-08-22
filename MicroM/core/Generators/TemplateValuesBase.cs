using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Generators
{
    internal abstract class TemplateValuesBase
    {
        public readonly Dictionary<string, string> tokens = [];

        public TemplateValuesBase()
        {
            FillTokens();
        }

        // MMC: this aims to ease the use of the tokens dictionary
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
