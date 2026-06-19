using MicroM.DataDictionary.Configuration;

namespace MicroM.Configuration.CategoriesDefinitions;

public class FileStorageTypes : CategoryDefinition
{
    public FileStorageTypes() : base("MicroM File Storage Types") { }

    public readonly CategoryValuesDefinition LocalFileStorage = new("Stores files locally on the server");
    public readonly CategoryValuesDefinition SQLFileStorage = new("Stores files in SQL DB");
}
