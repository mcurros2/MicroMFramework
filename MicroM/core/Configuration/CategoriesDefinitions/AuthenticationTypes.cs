using MicroM.DataDictionary.Configuration;

namespace MicroM.Configuration.CategoriesDefinitions;

public class AuthenticationTypes : CategoryDefinition
{

    public AuthenticationTypes() : base("MicroM Authentication Types") { }

    public readonly CategoryValuesDefinition SQLServerAuthentication = new("Authenticates using a SQL Server Login");
    public readonly CategoryValuesDefinition MicroMAuthentication = new($"Authenticates using MicroM");
    public readonly CategoryValuesDefinition SecretsAuthentication = new($"Authenticates using Secrets");
    public readonly CategoryValuesDefinition ADAuthentication = new($"Authenticates using Active Directory");

}
