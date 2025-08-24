namespace MicroM.Data
{
    public class DataResult
    {
        public string[] Header { get; private set; }

        public string[] typeInfo { get; private set; }

        public List<object?[]> records { get; private set; }

        public DataResult(int columns)
        {
            Header = new string[columns];
            typeInfo = new string[columns];
            records = [];
        }

        public DataResult(string[] headers, string[] type_info)
        {
            if(headers.Length != type_info.Length)
            {
                throw new ArgumentException("The headers and type_info arrays must have the same length");
            }
            Header = headers;
            typeInfo = type_info;
            records = [];
        }

        public object? this[int record, string key]
        {
            get
            {
                for (int x = 0; x < Header.Length; x++)
                {
                    if (Header[x].Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return records[x];
                    }
                }
                throw new ArgumentException($"The record has no column with the name {key}");
            }
        }


    }

    public class DataResult<T>
    {
        public string[] Header { get; private set; }

        public List<T> records { get; private set; }

        public DataResult(int columns)
        {
            Header = new string[columns];
            records = [];
        }


    }

}
