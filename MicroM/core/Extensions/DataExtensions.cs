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
        public static bool HasData(this List<DataResult>? result)
        {
            if (result != null && result.Count > 0 && result[0].records.Count > 0) return true;
            return false;
        }

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
        /// Returns the column value for <paramref name="column_name"/> for record_index <paramref name="record"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="column_name"></param>
        /// <param name="record"></param>
        /// <returns></returns>
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
