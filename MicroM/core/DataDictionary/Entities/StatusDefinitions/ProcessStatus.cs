using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.StatusDefs
{
    /// <summary>
    /// Basic status values to track the execution of a process.
    /// </summary>
    public class ProcessStatus : StatusDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessStatus"/> class.
        /// </summary>
        public ProcessStatus() : base("Process Status") { }

        /// <summary>Process has started.</summary>
        public readonly StatusValuesDefinition Started = new("Started", true);

        /// <summary>Process finished successfully.</summary>
        public readonly StatusValuesDefinition Completed = new("Completed");
    }
}
