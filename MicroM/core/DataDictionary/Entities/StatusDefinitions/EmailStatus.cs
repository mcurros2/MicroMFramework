
using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.StatusDefs
{
    public class EmailStatus : StatusDefinition
    {
        public EmailStatus() : base("EmailStatus") { }

        public readonly StatusValuesDefinition QUEUED = new("Queued", true);
        public readonly StatusValuesDefinition PROCESSING = new("Processing");
        public readonly StatusValuesDefinition SENT = new("Sent");
        public readonly StatusValuesDefinition ERROR = new("Error");
        public readonly StatusValuesDefinition RETRY = new("Pending retry");
    }
}
