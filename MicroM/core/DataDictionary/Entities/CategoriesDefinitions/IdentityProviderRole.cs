using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.CategoriesDefinitions;

/// <summary>
/// Provides category definitions for identity provider roles.
/// </summary>
public class IdentityProviderRole : CategoryDefinition
{
    /// <summary>
    /// Initializes identity provider role definitions with built-in values.
    /// </summary>
    public IdentityProviderRole() : base("Identity Provider Roles") { }

    /// <summary>
    /// Local application identity provider.
    /// </summary>
    public readonly CategoryValuesDefinition IDPDisabled = new("Local application identity provider");

    /// <summary>
    /// Identity Provider Client.
    /// </summary>
    public readonly CategoryValuesDefinition IDPClient = new("Identity Provider Client");

    /// <summary>
    /// Identity Provider Server.
    /// </summary>
    public readonly CategoryValuesDefinition IDPServer = new("Identity Provider Server");
}
