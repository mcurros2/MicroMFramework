using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.CategoriesDefinitions
{
    public class UserTypes : CategoryDefinition
    {

        public UserTypes() : base("User Types") { }

        public readonly CategoryValuesDefinition ADMIN = new("System Admin");
        public readonly CategoryValuesDefinition USER = new("User");

    }
}
