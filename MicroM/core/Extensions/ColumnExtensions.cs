using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Extensions
{
    public static class ColumnExtensions
    {
        public static CustomOrderedDictionary<ColumnBase> GetWithFlags(this IReadonlyOrderedDictionary<ColumnBase> cols, ColumnFlags flags, ColumnFlags exclude_flags = ColumnFlags.Fake, params string[] exclude_names)
        {
            var ret = new CustomOrderedDictionary<ColumnBase>();
            foreach (ColumnBase col in cols.Values)
            {
                if (col.Name.IsIn(exclude_names)) continue;
                if (col.ColumnMetadata.HasAnyFlag(exclude_flags) && exclude_flags != ColumnFlags.None) continue;
                if (col.ColumnMetadata.HasAnyFlag(flags) || flags == ColumnFlags.All) ret.Add(col.Name, col);
            }
            return ret;
        }

        public static void SetColumnValuesByName<T>(this T cols, IReadonlyOrderedDictionary<ColumnBase> values, params string[] names_to_copy) where T : Dictionary<string, ColumnBase>, IReadonlyOrderedDictionary<ColumnBase>
        {
            foreach (ColumnBase value in values.Values)
            {
                if (!value.Name.IsIn(names_to_copy)) continue;
                if (cols.Contains(value.Name)) cols[value.Name].ValueObject = value.ValueObject;
            }
        }

        public static void CopyColumnValuesByName(this IReadonlyOrderedDictionary<ColumnBase> cols, IReadonlyOrderedDictionary<ColumnBase> source)
        {
            foreach (ColumnBase value in source.Values)
            {
                if (cols.Contains(value.Name)) cols[value.Name]!.ValueObject = value.ValueObject;
            }
        }


        public static IEnumerable<ColumnBase> GetParmsWithFlags(this IReadonlyOrderedDictionary<ColumnBase> cols, ColumnFlags flags)
        {
            return cols.GetWithFlags(flags, ColumnFlags.None).Values;
        }

        public static void AddExisting(this CustomOrderedDictionary<ColumnBase> collection, ColumnBase col)
        {
            collection.Add(col.Name, col);
        }

        public static bool ContainsAllKeys(this IReadonlyOrderedDictionary<ColumnBase> source, IReadonlyOrderedDictionary<ColumnBase> cols)
        {
            foreach (string key in cols.Keys)
            {
                if (!source.Contains(key)) return false;
            }
            return true;
        }

        public static bool ContainsAllKeys(this IReadonlyOrderedDictionary<ColumnBase> source, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (!source.Contains(key)) return false;
            }
            return true;
        }

        public static void EncryptColumnData(this IEnumerable<ColumnBase> cols, IMicroMEncryption encryptor)
        {
            foreach (ColumnBase col in cols)
            {
                if (col.ValueObject != null && col.SystemType == typeof(string) && col.SQLMetadata.Encrypted && col.SQLMetadata.SQLType.IsIn(SqlDbType.VarChar, SqlDbType.Char, SqlDbType.NVarChar, SqlDbType.NChar, SqlDbType.Text, SqlDbType.NText))
                {
                    if (!string.IsNullOrEmpty((string)col.ValueObject)) col.ValueObject = encryptor.Encrypt((string)col.ValueObject);
                }
            }
        }

        public static void DecryptColumnData(this IEnumerable<ColumnBase> cols, IMicroMEncryption encryptor)
        {
            foreach (ColumnBase col in cols)
            {
                if (col.ValueObject != null && col.SQLMetadata.Encrypted && col.SQLMetadata.SQLType.IsIn(SqlDbType.VarChar, SqlDbType.Char, SqlDbType.NVarChar, SqlDbType.NChar, SqlDbType.Text, SqlDbType.NText))
                {
                    col.ValueObject = encryptor.Decrypt((string)col.ValueObject);
                }
            }
        }

        public static T MapColumnData<T>(this IReadonlyOrderedDictionary<ColumnBase> cols) where T : class, new()
        {
            var members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(p => p.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && p.GetCustomAttribute<CompilerGeneratedAttribute>() == null);

            T record = new();

            foreach (var member in members)
            {
                if (cols.Contains(member.Name))
                {
                    var col = cols[member.Name];
                    if (member is PropertyInfo prop)
                    {
                        prop.SetValue(record, col!.ValueObject);
                    }
                    else if (member is FieldInfo field)
                    {
                        field.SetValue(record, col!.ValueObject);
                    }

                }

            }

            return record;
        }


        public static Column<T> AddCol<T>(this CustomOrderedDictionary<ColumnBase> collection, string name, SqlDbType? sql_type, int size = 0, byte precision = 0, byte scale = 0,
            T value = default!, bool output = false, ColumnFlags column_flags = ColumnFlags.Insert | ColumnFlags.Update)
        {
            Column<T> col = new(name, value, sql_type, size, precision, scale, output, column_flags);
            collection.Add(col.Name, col);
            return col;
        }

        public static Column<T> AddPK<T>(this CustomOrderedDictionary<ColumnBase> collection, string name, SqlDbType? sql_type = SqlDbType.Char, int size = 20, byte precision = 0, byte scale = 0,
            T value = default!, bool autonum = false)
        {
            ColumnFlags col_flags = ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.Get | ColumnFlags.PK;
            if (autonum) col_flags |= ColumnFlags.Autonum;
            Column<T> col = new(name, value: value, sql_type: sql_type, size: size, precision: precision,
                scale: scale, column_flags: col_flags);
            collection.Add(col.Name, col);
            return col;
        }

        public static Column<T> AddFK<T>(this CustomOrderedDictionary<ColumnBase> collection, string name, SqlDbType? sql_type = SqlDbType.Char, int size = 20, byte precision = 0, byte scale = 0, T value = default!)
        {
            return collection.AddCol(name, value: value, sql_type: sql_type, size: size, precision: precision,
                scale: scale, column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.Get | ColumnFlags.FK);
        }

        public static SqlParameter CreateSQLParameter(this ColumnBase sql_col)
        {
            var parm = new SqlParameter
            {
                ParameterName = sql_col.SQLParameterName,
                SqlDbType = sql_col.SQLMetadata.SQLType,
                Size = sql_col.SQLMetadata.Size,
                Precision = sql_col.SQLMetadata.Precision,
                IsNullable = true
            };
            if (parm.SqlDbType == SqlDbType.DateTime)
            {
                parm.Value = (sql_col.ValueObject == null || (DateTime)sql_col.ValueObject == DateTime.MinValue) ? DBNull.Value : sql_col.ValueObject;
            }
            else
            {
                parm.Value = sql_col.ValueObject ?? DBNull.Value;
            }
            parm.Direction = (sql_col.SQLMetadata.Output) ? ParameterDirection.InputOutput : ParameterDirection.Input;
            return parm;
        }


        public static SqlParameter[] AsSqlParameters(this IEnumerable<ColumnBase> sql_cols)
        {
            var ret = new List<SqlParameter>();
            foreach (ColumnBase sql_col in sql_cols)
            {
                ret.Add(sql_col.CreateSQLParameter());
            }
            return [.. ret];
        }

        public static string StripColumnPrefix(this string column_name)
        {
            int prefix_idx = column_name.IndexOf('_');
            if (prefix_idx > -1)
            {
                prefix_idx++;
                string prefix = column_name[..prefix_idx];
                if (prefix.IsIn(SqlDbTypeMapper.ColumnPrefixes)) column_name = column_name[prefix_idx..];
            }
            return column_name;
        }

        public static ViewParm AsViewItemParm(this ColumnBase column, int column_mapping = -1, string compound_group = "", int compound_position = -1, bool compound_key = false, bool browsing_key = false)
        {
            Type SQLColType = typeof(Column<>).MakeGenericType(column.SystemType);
            var col = (ColumnBase?)Activator.CreateInstance(SQLColType, column, default, default) ?? throw new ArgumentException($"Unable to create a {SQLColType.Name} from {column.SystemType.Name}.");

            return new ViewParm(col, column_mapping, compound_group, compound_position, compound_key, browsing_key);
        }

        public static Dictionary<string, object?> ToDictionary(this IReadonlyOrderedDictionary<ColumnBase> cols, HashSet<string>? exclude_colnames = null)
        {
            Dictionary<string, object?> ret = new(StringComparer.OrdinalIgnoreCase);

            foreach (ColumnBase col in cols)
            {
                if (exclude_colnames != null && !exclude_colnames.Contains(col.Name))
                    ret.Add(col.Name, col.ValueObject);
            }

            return ret;
        }

        public static Dictionary<string, ColumnBase> ToColumnsDictionary(this IReadonlyOrderedDictionary<ColumnBase> cols, HashSet<string>? exclude_colnames = null)
        {
            Dictionary<string, ColumnBase> ret = new(StringComparer.OrdinalIgnoreCase);

            foreach (ColumnBase col in cols)
            {
                if (exclude_colnames != null && !exclude_colnames.Contains(col.Name))
                    ret.Add(col.Name, col);
            }

            return ret;
        }


        public static bool TryConvertFromString(this ColumnBase col, string? value_to_convert, out object? converted_value)
        {
            bool ret = false;
            converted_value = null;

            switch (col.SQLMetadata.SQLType)
            {
                case SqlDbType.VarChar:
                case SqlDbType.Char:
                case SqlDbType.NVarChar:
                case SqlDbType.NChar:
                case SqlDbType.Text:
                case SqlDbType.NText:
                case SqlDbType.Xml:
                    {
                        if (value_to_convert?.Length <= col.SQLMetadata.Size)
                        {
                            ret = true;
                            converted_value = value_to_convert;
                        }
                    }
                    break;
                case SqlDbType.Time:
                    {
                        ret = TimeSpan.TryParse(value_to_convert, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan result);
                        if (ret) converted_value = result;

                    }
                    break;
                case SqlDbType.DateTime:
                case SqlDbType.Date:
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTime2:
                    {
                        ret = DateTime.TryParse(value_to_convert, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault | System.Globalization.DateTimeStyles.RoundtripKind, out DateTime result);
                        if (ret) converted_value = result;
                    }
                    break;
                case SqlDbType.Int:
                    {
                        ret = int.TryParse(value_to_convert, out int result);
                        if (ret) converted_value = result;
                    }
                    break;
                case SqlDbType.TinyInt:
                    {
                        ret = byte.TryParse(value_to_convert, out byte result);
                        if (ret) converted_value = result;
                    }
                    break;
                case SqlDbType.BigInt:
                    {
                        ret = long.TryParse(value_to_convert, out long result);
                        if (ret) converted_value = result;
                    }
                    break;
                case SqlDbType.Float:
                    {
                        ret = double.TryParse(value_to_convert, out double result);
                        if (ret) converted_value = result;
                    }
                    break;
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    {
                        ret = decimal.TryParse(value_to_convert, out decimal result);
                        if (ret) converted_value = result;
                    }
                    break;
                case SqlDbType.UniqueIdentifier:
                    {
                        if (value_to_convert != null)
                        {
                            string value = value_to_convert.Trim();
                            ret = Guid.TryParse(value, out Guid result);
                            if (ret) converted_value = result;
                        }
                    }
                    break;
                case SqlDbType.Bit:
                    {
                        if (value_to_convert != null)
                        {
                            string value = value_to_convert.Trim();
                            if (value.IsIn(parms: new[] { "0", "1", bool.TrueString, bool.FalseString }, StringComparer.OrdinalIgnoreCase))
                            {
                                ret = true;
                                converted_value = value.IsIn("1", bool.TrueString);
                            }
                        }
                    }
                    break;
                case SqlDbType.Real:
                    {
                        ret = float.TryParse(value_to_convert, out float result);
                        if (ret) converted_value = result;
                    }
                    break;
            }

            return ret;
        }


        public static bool TryConvertFromJsonElement<T>(this JsonElement source, out T result)
        {
            var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var converter = TypeDescriptor.GetConverter(type);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    var jsonString = source.GetRawText();

                    // If the JSON value is a string, remove surrounding quotes
                    if (source.ValueKind == JsonValueKind.String)
                    {
                        jsonString = jsonString.Trim('"');
                    }

                    result = (T)converter.ConvertFromInvariantString(jsonString)!;
                    return true;
                }
                catch (Exception)
                {
                    // Conversion failed
                }
            }

            result = default!;
            return false;
        }

        public static bool TryConvertFromJsonElement(this JsonElement element, ColumnBase col, out object? converted_value)
        {
            bool ret = false;
            converted_value = null;

            switch (col.SQLMetadata.SQLType)
            {
                case SqlDbType.VarChar:
                case SqlDbType.Char:
                case SqlDbType.NVarChar:
                case SqlDbType.NChar:
                case SqlDbType.Text:
                case SqlDbType.NText:
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            ret = true;
                            converted_value = element.GetString();
                        }
                        else
                        {
                            ret = true;
                            converted_value = element.GetRawText();
                        }
                    }
                    break;
                case SqlDbType.Time:
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            ret = element.TryConvertFromJsonElement(out TimeSpan result);
                            if (ret) converted_value = result;
                        }
                    }
                    break;
                case SqlDbType.DateTime:
                case SqlDbType.Date:
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTime2:
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            ret = element.TryGetDateTime(out DateTime result);
                            if (ret) converted_value = result;
                        }
                    }
                    break;
                case SqlDbType.Int:
                    {
                        if (element.ValueKind == JsonValueKind.Number)
                        {
                            ret = element.TryGetInt32(out int result);
                            if (ret) converted_value = result;
                        }
                    }
                    break;
                case SqlDbType.TinyInt:
                    {
                        if (element.ValueKind == JsonValueKind.Number)
                        {
                            ret = element.TryGetByte(out byte result);
                            if (ret) converted_value = result;
                        }
                    }
                    break;
                case SqlDbType.BigInt:
                    {
                        if (element.ValueKind == JsonValueKind.Number)
                        {
                            ret = element.TryGetInt64(out long result);
                            if (ret) converted_value = result;
                        }
                    }
                    break;
                case SqlDbType.Float:
                    {
                        if (element.ValueKind == JsonValueKind.Number)
                        {
                            ret = element.TryGetDouble(out double result);
                            if (ret) converted_value = result;
                        }
                    }
                    break;
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.Real:
                case SqlDbType.SmallMoney:
                    {
                        if (element.ValueKind == JsonValueKind.Number)
                        {
                            ret = element.TryGetDecimal(out decimal result);
                            if (ret) converted_value = result;
                        }
                    }
                    break;
                case SqlDbType.UniqueIdentifier:
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            var to_convert = element.GetString();
                            if (to_convert != null)
                            {
                                string value = to_convert.Trim();
                                ret = Guid.TryParse(value, out Guid result);
                                if (ret) converted_value = result;
                            }
                        }
                    }
                    break;
                case SqlDbType.Bit:
                    {
                        if (element.ValueKind.IsIn(JsonValueKind.True, JsonValueKind.False))
                        {
                            ret = true;
                            converted_value = element.GetBoolean();
                        }
                    }
                    break;
            }

            return ret;
        }


    }


}
