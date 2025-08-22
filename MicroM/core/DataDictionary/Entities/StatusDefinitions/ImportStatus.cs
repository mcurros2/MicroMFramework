using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.StatusDefs
{
    /// <summary>
    /// Status values describing the progress of a data import operation.
    /// </summary>
    public class ImportStatus : StatusDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportStatus"/> class.
        /// </summary>
        public ImportStatus() : base("Import Status") { }

        /// <summary>Import has been queued but not started.</summary>
        public readonly StatusValuesDefinition Pending = new("Pending", true);

        /// <summary>The file format is invalid.</summary>
        public readonly StatusValuesDefinition FormatError = new("File format error");

        /// <summary>Import is currently running.</summary>
        public readonly StatusValuesDefinition Importing = new("Importing");

        /// <summary>Import completed successfully.</summary>
        public readonly StatusValuesDefinition Completed = new("Completed");

        /// <summary>An unexpected error occurred during import.</summary>
        public readonly StatusValuesDefinition Error = new("Error");
    }
}
