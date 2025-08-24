namespace MicroM.Data
{
    class DataAbstractionException : Exception
    {
        private readonly List<DBStatus> _StatusList = null!;
        public IEnumerable<DBStatus> StatusList { get => (IEnumerable<DBStatus>)StatusList.GetEnumerator(); }
        public DataAbstractionException() : base("Unexpected exception accessing data") { }

        public DataAbstractionException(string message, Exception? inner = null) : base(message, inner) { }

        public DataAbstractionException(string message, List<DBStatus> status_list) : base(message) { _StatusList = status_list; }

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
