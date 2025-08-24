
using MicroM.DataDictionary.Configuration;

namespace MicroM.DataDictionary.StatusDefs
{
    /// <summary>
    /// Status values representing the lifecycle of an email in the queue.
    /// </summary>
    public class EmailStatus : StatusDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailStatus"/> class.
        /// </summary>
        public EmailStatus() : base("EmailStatus") { }

        /// <summary>Queued but not yet processed.</summary>
        public readonly StatusValuesDefinition QUEUED = new("Queued", true);

        /// <summary>Currently being processed by the email sender.</summary>
        public readonly StatusValuesDefinition PROCESSING = new("Processing");

        /// <summary>Email was sent successfully.</summary>
        public readonly StatusValuesDefinition SENT = new("Sent");

        /// <summary>An error occurred while sending the email.</summary>
        public readonly StatusValuesDefinition ERROR = new("Error");

        /// <summary>The email failed but is pending a retry attempt.</summary>
        public readonly StatusValuesDefinition RETRY = new("Pending retry");
    }
}
