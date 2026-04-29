using MicroM.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MicroM.Extensions;

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
            if (string.IsNullOrWhiteSpace(value)) continue;
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

    private static Type ConvertSqlServerDataTypeNameToType(string sql_type)
    {
        // string
        if (sql_type.IsIn(
            SqlServerDataTypeNames.nchar,
            SqlServerDataTypeNames.nvarchar,
            SqlServerDataTypeNames.ntext,
            SqlServerDataTypeNames.@char,
            SqlServerDataTypeNames.varchar,
            SqlServerDataTypeNames.text,
            SqlServerDataTypeNames.xml
            ))
            return typeof(string);

        // bool
        if (sql_type == SqlServerDataTypeNames.bit)
            return typeof(bool);

        // integer numbers
        if (sql_type == SqlServerDataTypeNames.tinyint)
            return typeof(byte);

        if (sql_type == SqlServerDataTypeNames.smallint)
            return typeof(short);

        if (sql_type == SqlServerDataTypeNames.@int)
            return typeof(int);

        if (sql_type == SqlServerDataTypeNames.bigint)
            return typeof(long);

        // floating point
        if (sql_type == SqlServerDataTypeNames.real)
            return typeof(float);

        if (sql_type == SqlServerDataTypeNames.@float)
            return typeof(double);

        // exact numeric / money
        if (sql_type.IsIn(
            SqlServerDataTypeNames.@decimal,
            SqlServerDataTypeNames.numeric,
            SqlServerDataTypeNames.money,
            SqlServerDataTypeNames.smallmoney
            ))
            return typeof(decimal);

        // date/time
        if (sql_type.IsIn(
            SqlServerDataTypeNames.date,
            SqlServerDataTypeNames.datetime,
            SqlServerDataTypeNames.smalldatetime,
            SqlServerDataTypeNames.datetime2
            ))
            return typeof(DateTime);

        if (sql_type == SqlServerDataTypeNames.datetimeoffset)
            return typeof(DateTimeOffset);

        if (sql_type == SqlServerDataTypeNames.time)
            return typeof(TimeSpan);

        // binary
        if (sql_type.IsIn(
            SqlServerDataTypeNames.binary,
            SqlServerDataTypeNames.varbinary,
            SqlServerDataTypeNames.image,
            SqlServerDataTypeNames.rowversion,
            SqlServerDataTypeNames.timestamp
            ))
            return typeof(byte[]);

        // guid
        if (sql_type == SqlServerDataTypeNames.uniqueidentifier)
            return typeof(Guid);

        // provider/special types
        if (sql_type == SqlServerDataTypeNames.sql_variant)
            return typeof(object);

        if (sql_type.IsIn(
            SqlServerDataTypeNames.geography,
            SqlServerDataTypeNames.geometry,
            SqlServerDataTypeNames.hierarchyid,
            SqlServerDataTypeNames.cursor,
            SqlServerDataTypeNames.table
            ))
            return typeof(object);

        return typeof(object);

    }

    public static Type GetHeaderType(this DataResult result, int header_index)
    {
        if (result == null) return typeof(object);
        if (result.typeInfo == null) return typeof(object);
        if (header_index < 0 || header_index >= result.typeInfo.Length) return typeof(object);

        string t = result.typeInfo[header_index];
        if (string.IsNullOrWhiteSpace(t)) return typeof(object);

        return ConvertSqlServerDataTypeNameToType(t);
    }

    public static Type GetHeaderType(this DataResultChannel result, int header_index)
    {
        if (result == null) return typeof(object);
        if (result.typeInfo == null) return typeof(object);
        if (header_index < 0 || header_index >= result.typeInfo.Length) return typeof(object);

        string t = result.typeInfo[header_index];
        if (string.IsNullOrWhiteSpace(t)) return typeof(object);

        return ConvertSqlServerDataTypeNameToType(t);
    }

    /// <summary>
    /// Returns the column value for <paramref name="column_name"/> for record_index <paramref name="record"/>
    /// </summary>
    public static TColumn? Get<TColumn>(this DataResult result, string column_name, int record)
    {
        if (result == null) return default;
        for (int r = 0; r < result.Header.Length; r++)
        {
            if (result.Header[r].Equals(column_name, StringComparison.OrdinalIgnoreCase)) return (TColumn)result.records[record][r]!;
        }
        return default;
    }

    private static readonly string[] _sensitiveParamMarkers =
    [
        "password", "pwd", "vc_password", "vc_pwhash",
        "token", "access_token", "refresh", "vc_refreshtoken", "logout_token", "id_token", "authorization",
        "recovery_code"
    ];

    private static bool IsSensitiveParam(SqlParameter parm)
    {
        var name = parm.ParameterName ?? string.Empty;
        foreach (var marker in _sensitiveParamMarkers)
        {
            if (name.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return parm.SqlDbType is SqlDbType.Binary or SqlDbType.VarBinary or SqlDbType.Image;
    }

    private static string TraceSQLParm(this SqlParameter parm)
    {
        if (IsSensitiveParam(parm))
        {
            var len = parm.Value.ToString()?.Length ?? 0;
            return $"{parm.ParameterName} = '[SENSITIVE len={len}]'";
        }

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

    public static bool IsNullOrFailed(this DBStatusResult? status)
    {
        return status == null || status.Failed;
    }

    public static void ThrowIfFailed(this DBStatusResult? status, string? message = null)
    {
        if (status == null) throw new InvalidOperationException(message ?? "DBStatusResult is null");
        if (status.Failed) throw new DataAbstractionException(message ?? "DB operation failed", status.Results!);
    }

}
