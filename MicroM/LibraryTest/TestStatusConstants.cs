using MicroM.DataDictionary.Configuration;

namespace LibraryTest.DataDictionary
{
    internal class QUEUE : StatusDefinition
    {
        public QUEUE() : base("test status description") { }

        public readonly StatusValuesDefinition DRAFT = new("Draft", true);
        public readonly StatusValuesDefinition PEND = new("Pending");
        public readonly StatusValuesDefinition PROCESSING = new("Processing");
        public readonly StatusValuesDefinition SUCCESS = new("Success");
        public readonly StatusValuesDefinition FAILURE = new("Failure");
            
    }
}
