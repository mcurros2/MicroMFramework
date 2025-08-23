namespace MicroM.Data
{
    /// <summary>
    /// Represents errors that occur when interacting with the data abstraction layer.
    /// </summary>
    class DataAbstractionException : Exception
    {
        /// <summary>
        /// List of database status results associated with the exception.
        /// </summary>
        private readonly List<DBStatus> _StatusList = null!;

        /// <summary>
        /// Gets the collection of database status messages associated with the exception.
        /// </summary>
        public IEnumerable<DBStatus> StatusList { get => (IEnumerable<DBStatus>)StatusList.GetEnumerator(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAbstractionException"/> class.
        /// </summary>
        public DataAbstractionException() : base("Unexpected exception accessing data") { }

        /// <summary>
        /// Initializes a new instance with a specified error message and optional inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public DataAbstractionException(string message, Exception? inner = null) : base(message, inner) { }

        /// <summary>
        /// Initializes a new instance with the specified message and database status list.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="status_list">The list of database status entries that caused the exception.</param>
        public DataAbstractionException(string message, List<DBStatus> status_list) : base(message) { _StatusList = status_list; }

        /// <summary>
        /// Gets the error message that explains the reason for the exception, including status details when available.
        /// </summary>
        public override string Message
        {
            get
            {
                string ret = base.Message;
                if (_StatusList != null && _StatusList.Count > 0)
                {
                    foreach (var stat in _StatusList)
                    {
                        ret += $"\nStatus: {stat.Status}, '{stat.Message}'";
                    }
                }
                return ret;
            }
        }

    }
}
