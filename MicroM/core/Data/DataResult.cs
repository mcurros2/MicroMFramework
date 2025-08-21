using System;
using System.Collections.Generic;

namespace MicroM.Data
{
    /// <summary>
    /// Represents a tabular result set with dynamic values.
    /// </summary>
    public class DataResult
    {
        /// <summary>
        /// Column names for the result set.
        /// </summary>
        public string[] Header { get; private set; }

        /// <summary>
        /// Type information for each column in <see cref="Header"/>.
        /// </summary>
        public string[] typeInfo { get; private set; }

        /// <summary>
        /// Collection of record values.
        /// </summary>
        public List<object?[]> records { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="DataResult"/> with a specified number of columns.
        /// </summary>
        /// <param name="columns">Number of columns in the result set.</param>
        public DataResult(int columns)
        {
            Header = new string[columns];
            typeInfo = new string[columns];
            records = [];
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DataResult"/> with headers and type information.
        /// </summary>
        /// <param name="headers">Column names.</param>
        /// <param name="type_info">Type information for each column.</param>
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

        /// <summary>
        /// Gets a value from the specified record and column key.
        /// </summary>
        /// <param name="record">Record index.</param>
        /// <param name="key">Column name.</param>
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

    /// <summary>
    /// Represents a tabular result set with strongly typed records.
    /// </summary>
    /// <typeparam name="T">Type of the records contained in the result set.</typeparam>
    public class DataResult<T>
    {
        /// <summary>
        /// Column names for the result set.
        /// </summary>
        public string[] Header { get; private set; }

        /// <summary>
        /// Collection of strongly typed records.
        /// </summary>
        public List<T> records { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="DataResult{T}"/> with a specified number of columns.
        /// </summary>
        /// <param name="columns">Number of columns in the result set.</param>
        public DataResult(int columns)
        {
            Header = new string[columns];
            records = [];
        }
    }
}
