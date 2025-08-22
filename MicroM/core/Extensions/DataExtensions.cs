using MicroM.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MicroM.Extensions
{
    public static class DataExtensions
    {
        /// <summary>
        /// Determines whether the result set contains any records.
        /// </summary>
        /// <param name="result">Result list to inspect.</param>
        /// <returns><c>true</c> if data exists.</returns>
        public static bool HasData(this List<DataResult>? result)
        {
            if (result != null && result.Count > 0 && result[0].records.Count > 0) return true;
            return false;
        }

        /// <summary>
        /// Converts a record into a dictionary of string values keyed by header.
        /// </summary>
        /// <param name="result">Data result containing records.</param>
        /// <param name="record_index">Record index to convert.</param>
        /// <param name="comparer">Optional key comparer.</param>
        /// <returns>Dictionary of header names and string values.</returns>
        public static Dictionary<string, string> ToDictionaryOfStringRecord(this DataResult result, int record_index, StringComparer? comparer)
        {
            if (result.records.Count == 0) return [];
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(record_index, result.records.Count);

            comparer ??= StringComparer.OrdinalIgnoreCase;
            Dictionary<string, string> dict = new(comparer);

            for (int r = 0; r < result.Header.Length; r++)
            {
                dict[result.Header[r]] = result.records[record_index][r]?.ToString() ?? "";
            }

            return dict;
        }

        /// <summary>
        /// Returns a list of non-empty values from the specified column.
        /// </summary>
        /// <param name="result">Data result containing records.</param>
        /// <param name="header_index">Column index.</param>
        /// <returns>List of string values.</returns>
        public static List<string> ToListOfStringColumn(this DataResult result, int header_index)
        {
            if (result.records.Count == 0) return [];
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(header_index, result.Header.Length);

            List<string> column_records = [];

            for (int r = 0; r < result.records.Count; r++)
            {
                var value = result.records[r][header_index]?.ToString() ?? "";
                if(string.IsNullOrWhiteSpace(value)) continue;
                column_records.Add(value);
            }

            return column_records;
        }

        /// <summary>
        /// Converts a record into a dictionary of objects keyed by header.
        /// </summary>
        /// <param name="result">Data result.</param>
        /// <param name="record_index">Record index to convert.</param>
        /// <returns>Dictionary of header names and values.</returns>
        public static Dictionary<string, object> ToDictionary(this DataResult result, int record_index)
        {
            if (result.records.Count == 0) return [];
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(record_index, result.records.Count);

            Dictionary<string, object> dict = new(StringComparer.OrdinalIgnoreCase);

            for (int r = 0; r < result.Header.Length; r++)
            {
                var value = result.records[record_index][r] ?? DBNull.Value;
                dict[result.Header[r]] = value;
            }

            return dict;
        }

        /// <summary>
        /// Gets the header index for the specified column name.
        /// </summary>
        /// <param name="result">Data result.</param>
        /// <param name="column_name">Column name to search.</param>
        /// <returns>Index of the column or null if not found.</returns>
        public static int? GetHeaderIndex(this DataResult result, string column_name)
        {
            if (result == null) return null;
            for (int r = 0; r < result.Header.Length; r++)
            {
                if (result.Header[r].Equals(column_name, StringComparison.OrdinalIgnoreCase)) return r;
            }
            return null;
        }

        /// <summary>
        /// Retrieves a typed value from the specified record and column.
        /// </summary>
        /// <typeparam name="TColumn">Type of the value.</typeparam>
        /// <param name="result">Data result.</param>
        /// <param name="column_name">Column name to access.</param>
        /// <param name="record">Record index.</param>
        /// <returns>Value of the column or default.</returns>
        public static TColumn? Get<TColumn>(this DataResult result, string column_name, int record)
        {
            if (result == null) return default;
            for (int r = 0; r < result.Header.Length; r++)
            {
                if (result.Header[r].Equals(column_name, StringComparison.OrdinalIgnoreCase)) return (TColumn)result.records[record][r]!;
            }
            return default;
        }

        private static string TraceSQLParm(this SqlParameter parm)
        {
            if (parm.SqlDbType.IsIn(SqlDbType.Char, SqlDbType.NChar, SqlDbType.VarChar, SqlDbType.NVarChar, SqlDbType.Text, SqlDbType.NText))
            {
                return $"{parm.ParameterName} = '{parm.Value}'";
            }
            else
            {
                return $"{parm.ParameterName} = {parm.Value}";
            }
        }

        /// <summary>
        /// Produces a textual representation of a SQL command with parameters.
        /// </summary>
        /// <param name="cmd">SQL command.</param>
        /// <returns>Traceable SQL string.</returns>
        public static string TraceSQL(this SqlCommand cmd)
        {
            if (cmd.CommandType == CommandType.Text) return cmd.CommandText;
            if (cmd.CommandType == CommandType.TableDirect) return $"tabledirect: {cmd.CommandText}";

            string parms = "";
            if (cmd.Parameters.Count > 0)
            {
                StringBuilder sb = new();

                var penum = cmd.Parameters.GetEnumerator();
                penum.MoveNext();
                SqlParameter parm = (SqlParameter)penum.Current;
                sb.Append(parm.TraceSQLParm());
                while (penum.MoveNext())
                {
                    parm = (SqlParameter)penum.Current;
                    sb.AppendFormat(CultureInfo.InvariantCulture, ", {0}", parm.TraceSQLParm());
                }

                parms = sb.ToString();
            }

            return $"exec {cmd.CommandText} {parms}";
        }

        /// <summary>
        /// Parses a JSON string array into a string array.
        /// </summary>
        /// <param name="json_string_array">JSON string to parse.</param>
        /// <param name="dont_throw_exception">If true, returns null on parse error.</param>
        /// <returns>Array of strings or null.</returns>
        public static string[]? FromJsonStringArray(this string? json_string_array, bool dont_throw_exception = true)
        {
            if (string.IsNullOrWhiteSpace(json_string_array)) return null;
            try
            {
                return JsonSerializer.Deserialize<string[]>(json_string_array);
            }
            catch
            {
                if (dont_throw_exception) return null;
                else throw;
            }
        }


    }
}
