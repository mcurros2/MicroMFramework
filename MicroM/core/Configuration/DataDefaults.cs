namespace MicroM.Configuration
{
    /// <summary>
    /// Default values for data access operations.
    /// </summary>
    public static class DataDefaults
    {
        /// <summary>
        /// Connection timeout in seconds. Default = 15
        /// </summary>
        public static int DefaultConnectionTimeOutInSecs = 15;
        /// <summary>
        /// Query time out in minutes. Default = 3.
        /// </summary>
        public static int DefaultCommandTimeOutInMins = 3;
        /// <summary>
        /// Format to convert date time to SQL string
        /// </summary>
        public static string DateFormat = "yyyyMMdd HH:mm:ss";
        /// <summary>
        /// Max items for the channel that holds records for a result.
        /// If max items is reached the writer will wait until items are read from the channel.
        /// </summary>
        public static int DefaultChannelRecordsBuffer = 500;
        /// <summary>
        /// Max items for the channel that holds DataResultChannel in a DataResultSetChannel.
        /// If max items is reached the writer will wait until items are read from the channel.
        /// </summary>
        public static int DefaultChannelResultsBuffer = 10;
        /// <summary>
        /// Indicates to append the string "dbo." to a stored procedure if owner is not specified. Default = true
        /// </summary>
        public static bool AppendDBOtoProcs = true;

        /// <summary>
        /// The default limit for the returned rows from a view.
        /// </summary>
        public static int DefaultRowLimitForViews = 50;
        /// <summary>
        /// Parameter name for row_limit for use in executing views, sent along with values. This is a reserved word that can't be used as a column name for an entity
        /// </summary>
        public static string RowLimitParameterName = "@row_limit";
    }
}
