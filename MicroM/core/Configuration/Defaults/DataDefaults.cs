namespace MicroM.Configuration
{
    public static class DataDefaults
    {
        /// <summary>
        /// Connection timeout in seconds. Default = 5
        /// </summary>
        public static int DefaultConnectionTimeOutInSecs { get; set; } = 15;
        /// <summary>
        /// Query time out in minutes. Default = 3.
        /// </summary>
        public static int DefaultCommandTimeOutInMins { get; set; } = 3;
        /// <summary>
        /// Format to convert date time to SQL string
        /// </summary>
        public static string DateFormat { get; set; } = "yyyyMMdd HH:mm:ss";
        /// <summary>
        /// Max items for the channel that holds records for a result.
        /// If max items is reached the writer will wait until items are read from the channel.
        /// </summary>
        public static int DefaultChannelRecordsBuffer { get; set; } = 64 * 1024;
        /// <summary>
        /// Gets or sets the default buffer size, in rows, used when exporting data to Excel channels.
        /// </summary>
        /// <remarks>Adjusting this value can affect the performance and memory usage of the export
        /// operation. Larger buffer sizes may improve performance for large data sets but increase memory consumption.
        /// Set this value based on the expected size of the data being exported and available system
        /// resources.</remarks>
        public static int DefaultChannelExportToExcelBuffer { get; set; } = 64 * 1024;

        /// <summary>
        /// Gets or sets the default initial capacity, in bytes, for the file stream used when exporting data to an
        /// Excel file.
        /// </summary>
        public static int DefaultExportToExcelFileStreamCapacity { get; set; } = 64 * 1024;

        /// <summary>
        /// Gets or sets the default initial capacity of the shared string dictionary used during Excel export
        /// operations.
        /// </summary>
        public static int DefaultExportToExcelSharedStringDictionaryCapacity { get; set; } = 64 * 1024;

        /// <summary>
        /// Gets or sets a value indicating whether inline strings are used when exporting data to Excel.
        /// </summary>
        public static bool DefaultExportExcelUseInlineStrings { get; set; } = false;

        /// <summary>
        /// Max items for the channel that holds DataResultChannel in a DataResultSetChannel.
        /// If max items is reached the writer will wait until items are read from the channel.
        /// </summary>
        public static int DefaultChannelResultsBuffer { get; set; } = 2;
        /// <summary>
        /// Indicates to append the string "dbo." to a stored procedure if owner is not specified. Default = true
        /// </summary>
        public static bool AppendDBOtoProcs { get; set; } = true;

        /// <summary>
        /// The default limit for the returned rows from a view.
        /// </summary>
        public static int DefaultRowLimitForViews { get; set; } = 50;
        /// <summary>
        /// Parameter name for row_limit for use in executing views, sent along with values. This is a reserved word that can't be used as a column name for an entity
        /// </summary>
        public static string RowLimitParameterName { get; set; } = "@row_limit";

        /// <summary>
        /// Gets or sets the schema name for Data Dictionary tables and stored procedures. Default is null (uses dbo).
        /// </summary>
        public static string? DataDictionarySchema { get; set; } = null;
    }
}
