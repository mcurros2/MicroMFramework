using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.StatusDefinitions;

public class ProcessStatus : StatusDefinition
{
    public ProcessStatus() : base("Process Status") { }

    public readonly StatusValuesDefinition Started = new("Started", true);
    public readonly StatusValuesDefinition Completed = new("Completed");

}
