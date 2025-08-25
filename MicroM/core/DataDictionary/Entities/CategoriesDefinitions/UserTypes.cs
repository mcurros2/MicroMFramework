using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.CategoriesDefinitions;

/// <summary>
/// Provides category definitions for MicroM user types.
/// </summary>
public class UserTypes : CategoryDefinition
{
    /// <summary>
    /// Initializes user type definitions with built-in values.
    /// </summary>
    public UserTypes() : base("User Types") { }

    /// <summary>
    /// System administrator.
    /// </summary>
    public readonly CategoryValuesDefinition ADMIN = new("System Admin");

    /// <summary>
    /// General user.
    /// </summary>
    public readonly CategoryValuesDefinition USER = new("User");
}
