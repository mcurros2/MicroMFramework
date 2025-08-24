
using System.Text.Json.Serialization;

namespace MicroM.Data
{
    /// <summary>
    /// Represents the possible status codes returned from database operations.
    /// </summary>
    public enum DBStatusCodes
    {
        /// <summary>Operation completed successfully.</summary>
        OK = 0,
        /// <summary>The record was modified by another process.</summary>
        RecordHasChanged = 4,
        /// <summary>An unspecified error occurred.</summary>
        Error = 11,
        /// <summary>An auto-number value was returned.</summary>
        Autonum = 15
    }

    /// <summary>
    /// Details the status and optional message from a database call.
    /// </summary>
    public class DBStatus
    {
        /// <summary>
        /// Status code for the operation.
        /// </summary>
        public DBStatusCodes Status { get; init; }

        /// <summary>
        /// Optional descriptive message for the status.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; init; } = null;

        /// <summary>
        /// Initializes a new instance of <see cref="DBStatus"/> with a status code and optional message.
        /// </summary>
        /// <param name="status">Status code for the operation.</param>
        /// <param name="message">Optional descriptive message.</param>
        public DBStatus(DBStatusCodes status, string? message = null)
        {
            Status = status;
            Message = message;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DBStatus"/> with default values.
        /// </summary>
        public DBStatus() { }
    }

}
