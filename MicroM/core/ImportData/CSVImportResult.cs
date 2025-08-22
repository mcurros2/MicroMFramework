namespace MicroM.ImportData
{
    /// <summary>
    /// Represents the result of a CSV import operation.
    /// </summary>
    public class CSVImportResult
    {
        /// <summary>
        /// Gets or sets the number of rows processed.
        /// </summary>
        public int ProcessedCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of rows imported successfully.
        /// </summary>
        public int SuccessCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of rows that failed to import.
        /// </summary>
        public int ErrorCount { get; set; } = 0;

        /// <summary>
        /// Gets the import errors keyed by row number.
        /// </summary>
        public Dictionary<int, string> Errors { get; set; } = [];
    }
}
