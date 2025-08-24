namespace MicroM.Data
{
    /// <summary>
    /// Aggregates multiple <see cref="DBStatus"/> entries from a database operation.
    /// </summary>
    public class DBStatusResult
    {
        /// <summary>
        /// Indicates whether any of the statuses represent a failure.
        /// </summary>
        public bool Failed { get; init; } = false;

        /// <summary>
        /// Indicates whether an auto-number value was returned.
        /// </summary>
        public bool AutonumReturned { get; init; } = false;

        /// <summary>
        /// Collection of status entries returned by the database.
        /// </summary>
        public List<DBStatus>? Results { get; init; }

        /// <summary>
        /// Initializes a new instance of <see cref="DBStatusResult"/>.
        /// </summary>
        public DBStatusResult() { }
    }
}
