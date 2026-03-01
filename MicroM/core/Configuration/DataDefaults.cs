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
        public static int DefaultChannelRecordsBuffer { get; set; } = 5000;
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
    }
}
