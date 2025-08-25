using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.CategoriesDefinitions;

/// <summary>
/// Provides category definitions for supported authentication types.
/// </summary>
public class AuthenticationTypes : CategoryDefinition
{
    /// <summary>
    /// Initializes the authentication type definitions with built-in values.
    /// </summary>
    public AuthenticationTypes() : base("MicroM Authentication Types") { }

    /// <summary>
    /// Authenticates using a SQL Server Login.
    /// </summary>
    public readonly CategoryValuesDefinition SQLServerAuthentication = new("Authenticates using a SQL Server Login");

    /// <summary>
    /// Authenticates using MicroM.
    /// </summary>
    public readonly CategoryValuesDefinition MicroMAuthentication = new("Authenticates using MicroM");
}
