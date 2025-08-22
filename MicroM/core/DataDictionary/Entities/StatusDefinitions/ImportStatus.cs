using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.StatusDefs
{
    public class ImportStatus : StatusDefinition
    {
        public ImportStatus() : base("Import Status") { }

        public readonly StatusValuesDefinition Pending = new("Pending", true);
        public readonly StatusValuesDefinition FormatError = new("File format error");
        public readonly StatusValuesDefinition Importing = new("Importing");
        public readonly StatusValuesDefinition Completed = new("Completed");
        public readonly StatusValuesDefinition Error = new("Error");
    }
}
