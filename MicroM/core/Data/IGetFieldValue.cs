﻿using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace MicroM.Data
{
    /// <summary>
    /// Provides access to the reader functions to get the underlying values
    /// </summary>
    public interface IGetFieldValue
    {
        public Task<T> GetFieldValueAsync<T>(int position, CancellationToken ct);
        public Task<T> GetFieldValueAsync<T>(string column_name, CancellationToken ct);
        public T GetFieldValue<T>(int position);
        public T GetFieldValue<T>(string column_name);

    }

    public class ValueReader(SqlDataReader reader) : IGetFieldValue
    {
        internal DbDataReader _reader = reader;

        /// <summary>
        /// See <see cref="DbDataReader.GetFieldValue{T}(int)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="position"></param>
        /// <returns></returns>
        public T GetFieldValue<T>(int position)
        {
            if (_reader.IsDBNull(position))
            {
                return default!;
            }
            return _reader.GetFieldValue<T>(position);
        }

        /// <summary>
        /// Calls <see cref="DbDataReader.GetFieldValue{T}(int)"/> with <see cref="DbDataReader.GetOrdinal(string)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column_name"></param>
        /// <returns></returns>
        public T GetFieldValue<T>(string column_name)
        {
            return this.GetFieldValue<T>(_reader.GetOrdinal(column_name));
        }

        /// <summary>
        /// Calls <see cref="DbDataReader.GetFieldValueAsync{T}(int, CancellationToken)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="position"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<T> GetFieldValueAsync<T>(int position, CancellationToken ct)
        {
            if (await _reader.IsDBNullAsync(position, ct))
            {
                return default!;
            }
            return await _reader.GetFieldValueAsync<T>(position, ct);
        }

        /// <summary>
        /// Calls <see cref="DbDataReader.GetFieldValueAsync{T}(int)"/> with <see cref="DbDataReader.GetOrdinal(string)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column_name"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<T> GetFieldValueAsync<T>(string column_name, CancellationToken ct)
        {
            return this.GetFieldValueAsync<T>(_reader.GetOrdinal(column_name), ct);
        }


    }

}
