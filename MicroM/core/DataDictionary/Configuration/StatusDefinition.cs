using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.DataDictionary.Configuration
{
    public abstract class StatusDefinition
    {
        public StatusDefinition() { }

        public string StatusID { get; private set; } = "";
        public string Description { get; init; } = "";

        public StatusDefinition(string description)
        {
            StatusID = this.GetType().Name;
            Description = description;
            FillValuesDictionary();
        }

        public Dictionary<string, StatusValuesDefinition> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        private void FillValuesDictionary()
        {
            IOrderedEnumerable<MemberInfo> instance_members = this.GetType().GetAndCacheInstanceMembers();

            bool initialValueDefined = false;

            foreach (var prop in instance_members)
            {
                if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                {
                    if (prop.GetMemberType() == typeof(StatusValuesDefinition))
                    {
                        var status_value = (StatusValuesDefinition?)prop.GetMemberValue(this);
                        if (status_value != null)
                        {
                            if (status_value.InitialValue && initialValueDefined)
                            {
                                throw new ArgumentException($"InitialValue already defined. Only one status value can be defined as initial value. Status Value: Value {status_value.StatusValueID} ({status_value.Description}), Status {this.StatusID} ({Description})");
                            }
                            if (Values.TryAdd(prop.Name, status_value))
                            {
                                status_value.StatusValueID = prop.Name;
                            }
                            else
                            {
                                throw new ArgumentException($"Duplicate Status Value: Value {status_value.StatusValueID} ({status_value.Description}), Status {this.StatusID} ({Description})");
                            }
                        }
                    }
                }
            }
        }

    }

}


