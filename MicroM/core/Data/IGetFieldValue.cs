using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace MicroM.Data
{
    /// <summary>
    /// Provides access to the reader functions to get the underlying values
    /// </summary>
    public interface IGetFieldValue
    {
        /// <summary>
        /// Asynchronously retrieves a field value by its column position.
        /// </summary>
        /// <typeparam name="T">Expected return type.</typeparam>
        /// <param name="position">Zero-based column ordinal.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The value cast to <typeparamref name="T"/>.</returns>
        public Task<T> GetFieldValueAsync<T>(int position, CancellationToken ct);

        /// <summary>
        /// Asynchronously retrieves a field value by its column name.
        /// </summary>
        /// <typeparam name="T">Expected return type.</typeparam>
        /// <param name="column_name">Column name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The value cast to <typeparamref name="T"/>.</returns>
        public Task<T> GetFieldValueAsync<T>(string column_name, CancellationToken ct);

        /// <summary>
        /// Retrieves a field value by its column position.
        /// </summary>
        /// <typeparam name="T">Expected return type.</typeparam>
        /// <param name="position">Zero-based column ordinal.</param>
        /// <returns>The value cast to <typeparamref name="T"/>.</returns>
        public T GetFieldValue<T>(int position);

        /// <summary>
        /// Retrieves a field value by its column name.
        /// </summary>
        /// <typeparam name="T">Expected return type.</typeparam>
        /// <param name="column_name">Column name.</param>
        /// <returns>The value cast to <typeparamref name="T"/>.</returns>
        public T GetFieldValue<T>(string column_name);

    }

    /// <summary>
    /// Provides an implementation of <see cref="IGetFieldValue"/> that wraps a
    /// <see cref="SqlDataReader"/> to retrieve typed column values.
    /// </summary>
    /// <remarks>
    /// Methods on this class delegate to the underlying <see cref="DbDataReader"/>
    /// while fulfilling the <see cref="IGetFieldValue"/> contract.
    /// </remarks>
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
