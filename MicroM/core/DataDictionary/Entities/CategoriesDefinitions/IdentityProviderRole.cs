using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.CategoriesDefinitions;

public class IdentityProviderRole : CategoryDefinition
{
    public IdentityProviderRole() : base("Identity Provider Roles") { }
    public readonly CategoryValuesDefinition IDPDisabled = new("Local application identity provider");
    public readonly CategoryValuesDefinition IDPClient = new("Identity Provider Client");
    public readonly CategoryValuesDefinition IDPServer = new("Identity Provider Server");
}
