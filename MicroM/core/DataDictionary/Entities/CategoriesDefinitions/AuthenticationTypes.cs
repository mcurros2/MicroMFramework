using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.CategoriesDefinitions
{
    public class AuthenticationTypes : CategoryDefinition
    {

        public AuthenticationTypes() : base("MicroM Authentication Types") { }

        public readonly CategoryValuesDefinition SQLServerAuthentication = new("Authenticates using a SQL Server Login");
        public readonly CategoryValuesDefinition MicroMAuthentication = new($"Authenticates using MicroM");
    }
}
