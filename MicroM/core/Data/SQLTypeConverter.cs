using MicroM.Extensions;
using System.Data;

namespace MicroM.Data
{
    public static class SqlDbTypeMapper
    {

        private static readonly Dictionary<SqlDbType, Type> SqlDbTypeToType = new()
        {
            { SqlDbType.BigInt, typeof(long) },
            { SqlDbType.Binary, typeof(byte[]) },
            { SqlDbType.Image, typeof(byte[]) },
            { SqlDbType.Timestamp, typeof(byte[]) },
            { SqlDbType.VarBinary, typeof(byte[]) },
            { SqlDbType.Bit, typeof(bool) },
            { SqlDbType.Char, typeof(string) },
            { SqlDbType.NChar, typeof(string) },
            { SqlDbType.NText, typeof(string) },
            { SqlDbType.NVarChar, typeof(string) },
            { SqlDbType.Text, typeof(string) },
            { SqlDbType.VarChar, typeof(string) },
            { SqlDbType.Xml, typeof(string) },
            { SqlDbType.DateTime, typeof(DateTime) },
            { SqlDbType.SmallDateTime, typeof(DateTime) },
            { SqlDbType.Date, typeof(DateOnly) },
            { SqlDbType.Time, typeof(TimeOnly) },
            { SqlDbType.DateTime2, typeof(DateTime) },
            { SqlDbType.Decimal, typeof(decimal) },
            { SqlDbType.Money, typeof(decimal) },
            { SqlDbType.SmallMoney, typeof(decimal) },
            { SqlDbType.Float, typeof(double) },
            { SqlDbType.Int, typeof(int) },
            { SqlDbType.Real, typeof(float) },
            { SqlDbType.UniqueIdentifier, typeof(Guid) },
            { SqlDbType.SmallInt, typeof(short) },
            { SqlDbType.TinyInt, typeof(byte) },
            { SqlDbType.Variant, typeof(object) },
            { SqlDbType.Udt, typeof(object) },
            { SqlDbType.Structured, typeof(DataTable) },
            { SqlDbType.DateTimeOffset, typeof(DateTimeOffset) }
        };

        private static readonly Dictionary<SqlDbType, Type> SqlDbTypeToNullableType = new()
        {
            { SqlDbType.BigInt, typeof(long?) },
            { SqlDbType.Binary, typeof(byte[]) },
            { SqlDbType.Image, typeof(byte[]) },
            { SqlDbType.Timestamp, typeof(byte[]) },
            { SqlDbType.VarBinary, typeof(byte[]) },
            { SqlDbType.Bit, typeof(bool?) },
            { SqlDbType.Char, typeof(string) },
            { SqlDbType.NChar, typeof(string) },
            { SqlDbType.NText, typeof(string) },
            { SqlDbType.NVarChar, typeof(string) },
            { SqlDbType.Text, typeof(string) },
            { SqlDbType.VarChar, typeof(string) },
            { SqlDbType.Xml, typeof(string) },
            { SqlDbType.DateTime, typeof(DateTime?) },
            { SqlDbType.SmallDateTime, typeof(DateTime?) },
            { SqlDbType.Date, typeof(DateOnly?) },
            { SqlDbType.Time, typeof(TimeOnly?) },
            { SqlDbType.DateTime2, typeof(DateTime?) },
            { SqlDbType.Decimal, typeof(decimal?) },
            { SqlDbType.Money, typeof(decimal?) },
            { SqlDbType.SmallMoney, typeof(decimal?) },
            { SqlDbType.Float, typeof(double?) },
            { SqlDbType.Int, typeof(int?) },
            { SqlDbType.Real, typeof(float?) },
            { SqlDbType.UniqueIdentifier, typeof(Guid?) },
            { SqlDbType.SmallInt, typeof(short?) },
            { SqlDbType.TinyInt, typeof(byte?) },
            { SqlDbType.Variant, typeof(object) },
            { SqlDbType.Udt, typeof(object) },
            { SqlDbType.Structured, typeof(DataTable) },
            { SqlDbType.DateTimeOffset, typeof(DateTimeOffset) }
        };

        private static readonly Dictionary<Type, SqlDbType[]> TypeToSQLDbType = new()
        {
            { typeof(bool), new[] { SqlDbType.Bit } },
            { typeof(bool?), new[] { SqlDbType.Bit } },
            { typeof(byte), new[] { SqlDbType.TinyInt } },
            { typeof(byte?), new[] { SqlDbType.TinyInt } },
            { typeof(string), new[] { SqlDbType.NVarChar, SqlDbType.VarChar, SqlDbType.Char, SqlDbType.NChar, SqlDbType.Text, SqlDbType.NText, SqlDbType.Xml } },
            { typeof(DateOnly), new[] { SqlDbType.Date } },
            { typeof(DateOnly?), new[] { SqlDbType.Date } },
            { typeof(TimeOnly), new[] { SqlDbType.Time } },
            { typeof(TimeOnly?), new[] { SqlDbType.Time } },
            { typeof(DateTime), new[] { SqlDbType.DateTime, SqlDbType.DateTime2, SqlDbType.Time, SqlDbType.SmallDateTime, SqlDbType.Date } },
            { typeof(DateTime?), new[] { SqlDbType.DateTime, SqlDbType.DateTime2, SqlDbType.Time,SqlDbType.SmallDateTime, SqlDbType.Date } },
            { typeof(DateTimeOffset), new[] { SqlDbType.DateTimeOffset } },
            { typeof(DateTimeOffset?), new[] { SqlDbType.DateTimeOffset } },
            { typeof(short), new[] { SqlDbType.SmallInt } },
            { typeof(short?), new[] { SqlDbType.SmallInt } },
            { typeof(int), new[] { SqlDbType.Int } },
            { typeof(int?), new[] { SqlDbType.Int } },
            { typeof(long), new[] { SqlDbType.BigInt } },
            { typeof(long?), new[] { SqlDbType.BigInt } },
            { typeof(decimal), new[] { SqlDbType.Decimal, SqlDbType.Money, SqlDbType.SmallMoney } },
            { typeof(decimal?), new[] { SqlDbType.Decimal, SqlDbType.Money, SqlDbType.SmallMoney } },
            { typeof(double), new[] { SqlDbType.Float } },
            { typeof(double?), new[] { SqlDbType.Float } },
            { typeof(float), new[] { SqlDbType.Real } },
            { typeof(float?), new[] { SqlDbType.Real } },
            { typeof(TimeSpan), new[] { SqlDbType.Time } },
            { typeof(TimeSpan?), new[] { SqlDbType.Time } },
            { typeof(Guid), new[] { SqlDbType.UniqueIdentifier } },
            { typeof(Guid?), new[] { SqlDbType.UniqueIdentifier } },
            { typeof(byte[]), new[] { SqlDbType.VarBinary, SqlDbType.Binary, SqlDbType.Image, SqlDbType.Timestamp } },
            { typeof(byte?[]), new[] { SqlDbType.VarBinary, SqlDbType.Binary, SqlDbType.Image, SqlDbType.Timestamp } },
            { typeof(DataTable), new[] { SqlDbType.Structured } },
            { typeof(object), new[] { SqlDbType.Variant, SqlDbType.Udt } },
            { typeof(char[]), new[] { SqlDbType.Char } },
            { typeof(char?[]), new[] { SqlDbType.Char } },
            { typeof(string[]), new[] { SqlDbType.NVarChar, SqlDbType.VarChar } },
        };

        private static readonly Dictionary<SqlDbType, string> SqlDbTypeToPrefix = new()
        {
            { SqlDbType.BigInt, "bi_" },
            { SqlDbType.Binary, "b_" },
            { SqlDbType.Image, "im_" },
            { SqlDbType.Timestamp, "ts_" },
            { SqlDbType.VarBinary, "vb_" },
            { SqlDbType.Bit, "bt_" },
            { SqlDbType.Char, "c_" },
            { SqlDbType.NChar, "nc_" },
            { SqlDbType.NText, "ntx_" },
            { SqlDbType.NVarChar, "nvc_" },
            { SqlDbType.Text, "tx_" },
            { SqlDbType.VarChar, "vc_" },
            { SqlDbType.Xml, "x_" },
            { SqlDbType.DateTime, "dt_" },
            { SqlDbType.SmallDateTime, "sdt_" },
            { SqlDbType.Date, "d_" },
            { SqlDbType.Time, "t_" },
            { SqlDbType.DateTime2, "dt2_" },
            { SqlDbType.Decimal, "n_" },
            { SqlDbType.Money, "m_" },
            { SqlDbType.SmallMoney, "sm_" },
            { SqlDbType.Float, "f_" },
            { SqlDbType.Int, "i_" },
            { SqlDbType.Real, "r_" },
            { SqlDbType.UniqueIdentifier, "ui_" },
            { SqlDbType.SmallInt, "si_" },
            { SqlDbType.TinyInt, "ti_" },
            { SqlDbType.Variant, "vr_" },
            { SqlDbType.Udt, "udt_" },
            { SqlDbType.Structured, "st_" },
            { SqlDbType.DateTimeOffset, "dto_" }

        };

        private static string[] _ColumnPrefixes = null!;
        public static string[] ColumnPrefixes
        {
            get
            {
                _ColumnPrefixes ??= [.. SqlDbTypeToPrefix.Values];

                return _ColumnPrefixes;
            }
        }

        public static Type ToClrType(this SqlDbType sql_type)
        {
            if (SqlDbTypeToType.TryGetValue(sql_type, out Type? type)) return type;
            throw new ArgumentOutOfRangeException(nameof(sql_type), sql_type, "Cannot map the SqlDbType to Type");
        }

        public static Type ToNullableClrType(this SqlDbType sql_type)
        {
            if (SqlDbTypeToNullableType.TryGetValue(sql_type, out Type? type)) return type;
            throw new ArgumentOutOfRangeException(nameof(sql_type), sql_type, "Cannot map the SqlDbType to Nullable Type");
        }

        public static bool IsTypeAccepted(this SqlDbType sql_type, Type type)
        {
            if (TypeToSQLDbType.TryGetValue(type, out SqlDbType[]? sql_types)) return sql_type.IsIn(sql_types);
            return false;
        }

        public static SqlDbType ToSqlDbType(this Type type)
        {
            return TypeToSQLDbType[type][0];
            //throw new ArgumentOutOfRangeException(nameof(type), type, $"The type {nameof(type)} has no mapping to SqlDbType or has multiple possible SqlDbTypes");
        }

        public static string SQLPrefix(this SqlDbType type)
        {
            return SqlDbTypeToPrefix[type];
        }


    }

}
