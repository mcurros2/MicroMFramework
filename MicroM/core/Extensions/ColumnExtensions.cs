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
        /// <summary>
        /// Retrieves columns that match the specified <paramref name="flags"/> while excluding others.
        /// </summary>
        /// <param name="cols">Source column collection.</param>
        /// <param name="flags">Flags to include.</param>
        /// <param name="exclude_flags">Flags to exclude.</param>
        /// <param name="exclude_names">Column names to skip.</param>
        /// <returns>Dictionary with the matching columns.</returns>
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

        /// <summary>
        /// Copies column values from <paramref name="values"/> into <paramref name="cols"/> for the specified names.
        /// </summary>
        /// <typeparam name="T">Column collection type.</typeparam>
        /// <param name="cols">Target columns.</param>
        /// <param name="values">Source values.</param>
        /// <param name="names_to_copy">Column names to copy.</param>
        public static void SetColumnValuesByName<T>(this T cols, IReadonlyOrderedDictionary<ColumnBase> values, params string[] names_to_copy) where T : Dictionary<string, ColumnBase>, IReadonlyOrderedDictionary<ColumnBase>
        {
            foreach (ColumnBase value in values.Values)
            {
                if (!value.Name.IsIn(names_to_copy)) continue;
                if (cols.Contains(value.Name)) cols[value.Name].ValueObject = value.ValueObject;
            }
        }

        /// <summary>
        /// Copies column values from the <paramref name="source"/> dictionary to <paramref name="cols"/> when names match.
        /// </summary>
        /// <param name="cols">Destination columns.</param>
        /// <param name="source">Source columns.</param>
        public static void CopyColumnValuesByName(this IReadonlyOrderedDictionary<ColumnBase> cols, IReadonlyOrderedDictionary<ColumnBase> source)
        {
            foreach (ColumnBase value in source.Values)
            {
                if (cols.Contains(value.Name)) cols[value.Name]!.ValueObject = value.ValueObject;
            }
        }


        /// <summary>
        /// Returns columns that match the specified <paramref name="flags"/>.
        /// </summary>
        /// <param name="cols">Source columns.</param>
        /// <param name="flags">Flags to include.</param>
        /// <returns>Enumeration of matching columns.</returns>
        public static IEnumerable<ColumnBase> GetParmsWithFlags(this IReadonlyOrderedDictionary<ColumnBase> cols, ColumnFlags flags)
        {
            return cols.GetWithFlags(flags, ColumnFlags.None).Values;
        }

        /// <summary>
        /// Adds an existing column instance to the collection.
        /// </summary>
        /// <param name="collection">Target collection.</param>
        /// <param name="col">Column to add.</param>
        public static void AddExisting(this CustomOrderedDictionary<ColumnBase> collection, ColumnBase col)
        {
            collection.Add(col.Name, col);
        }

        /// <summary>
        /// Determines whether all keys from <paramref name="cols"/> exist in <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Source columns.</param>
        /// <param name="cols">Columns containing keys to check.</param>
        /// <returns><c>true</c> if all keys are present.</returns>
        public static bool ContainsAllKeys(this IReadonlyOrderedDictionary<ColumnBase> source, IReadonlyOrderedDictionary<ColumnBase> cols)
        {
            foreach (string key in cols.Keys)
            {
                if (!source.Contains(key)) return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether all provided <paramref name="keys"/> exist in <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Source columns.</param>
        /// <param name="keys">Keys to check.</param>
        /// <returns><c>true</c> if all keys are present.</returns>
        public static bool ContainsAllKeys(this IReadonlyOrderedDictionary<ColumnBase> source, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (!source.Contains(key)) return false;
            }
            return true;
        }

        /// <summary>
        /// Encrypts string column values using the provided <paramref name="encryptor"/>.
        /// </summary>
        /// <param name="cols">Columns to encrypt.</param>
        /// <param name="encryptor">Encryption service.</param>
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

        /// <summary>
        /// Decrypts string column values using the provided <paramref name="encryptor"/>.
        /// </summary>
        /// <param name="cols">Columns to decrypt.</param>
        /// <param name="encryptor">Encryption service.</param>
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

        /// <summary>
        /// Maps column values to a new instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Destination record type.</typeparam>
        /// <param name="cols">Source columns.</param>
        /// <returns>Instance of <typeparamref name="T"/> populated with column values.</returns>
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


        /// <summary>
        /// Adds a new column definition to the collection.
        /// </summary>
        /// <typeparam name="T">Column value type.</typeparam>
        /// <param name="collection">Target collection.</param>
        /// <param name="name">Column name.</param>
        /// <param name="sql_type">SQL type of the column.</param>
        /// <param name="size">Column size.</param>
        /// <param name="precision">Numeric precision.</param>
        /// <param name="scale">Numeric scale.</param>
        /// <param name="value">Initial value.</param>
        /// <param name="output">Whether the column is an output parameter.</param>
        /// <param name="column_flags">Flags describing column behavior.</param>
        /// <returns>The created column.</returns>
        public static Column<T> AddCol<T>(this CustomOrderedDictionary<ColumnBase> collection, string name, SqlDbType? sql_type, int size = 0, byte precision = 0, byte scale = 0,
            T value = default!, bool output = false, ColumnFlags column_flags = ColumnFlags.Insert | ColumnFlags.Update)
        {
            Column<T> col = new(name, value, sql_type, size, precision, scale, output, column_flags);
            collection.Add(col.Name, col);
            return col;
        }

        /// <summary>
        /// Adds a primary key column definition.
        /// </summary>
        /// <typeparam name="T">Column value type.</typeparam>
        /// <param name="collection">Target collection.</param>
        /// <param name="name">Column name.</param>
        /// <param name="sql_type">SQL type of the column.</param>
        /// <param name="size">Column size.</param>
        /// <param name="precision">Numeric precision.</param>
        /// <param name="scale">Numeric scale.</param>
        /// <param name="value">Initial value.</param>
        /// <param name="autonum">Whether the column is auto-numbered.</param>
        /// <returns>The created primary key column.</returns>
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

        /// <summary>
        /// Adds a foreign key column definition.
        /// </summary>
        /// <typeparam name="T">Column value type.</typeparam>
        /// <param name="collection">Target collection.</param>
        /// <param name="name">Column name.</param>
        /// <param name="sql_type">SQL type of the column.</param>
        /// <param name="size">Column size.</param>
        /// <param name="precision">Numeric precision.</param>
        /// <param name="scale">Numeric scale.</param>
        /// <param name="value">Initial value.</param>
        /// <returns>The created foreign key column.</returns>
        public static Column<T> AddFK<T>(this CustomOrderedDictionary<ColumnBase> collection, string name, SqlDbType? sql_type = SqlDbType.Char, int size = 20, byte precision = 0, byte scale = 0, T value = default!)
        {
            return collection.AddCol(name, value: value, sql_type: sql_type, size: size, precision: precision,
                scale: scale, column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.Get | ColumnFlags.FK);
        }

        /// <summary>
        /// Creates a <see cref="SqlParameter"/> from the specified column.
        /// </summary>
        /// <param name="sql_col">Column used to build the parameter.</param>
        /// <returns>The created parameter.</returns>
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


        /// <summary>
        /// Converts a collection of columns into an array of <see cref="SqlParameter"/>.
        /// </summary>
        /// <param name="sql_cols">Columns to convert.</param>
        /// <returns>Array of SQL parameters.</returns>
        public static SqlParameter[] AsSqlParameters(this IEnumerable<ColumnBase> sql_cols)
        {
            var ret = new List<SqlParameter>();
            foreach (ColumnBase sql_col in sql_cols)
            {
                ret.Add(sql_col.CreateSQLParameter());
            }
            return [.. ret];
        }

        /// <summary>
        /// Removes table prefix from a column name if present.
        /// </summary>
        /// <param name="column_name">Column name with optional prefix.</param>
        /// <returns>Column name without prefix.</returns>
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

        /// <summary>
        /// Converts a column to a <see cref="ViewParm"/> definition.
        /// </summary>
        /// <param name="column">Source column.</param>
        /// <param name="column_mapping">Optional column mapping.</param>
        /// <param name="compound_group">Compound group name.</param>
        /// <param name="compound_position">Position inside compound group.</param>
        /// <param name="compound_key">Indicates if part of compound key.</param>
        /// <param name="browsing_key">Indicates if used for browsing.</param>
        /// <returns>New <see cref="ViewParm"/> instance.</returns>
        public static ViewParm AsViewItemParm(this ColumnBase column, int column_mapping = -1, string compound_group = "", int compound_position = -1, bool compound_key = false, bool browsing_key = false)
        {
            Type SQLColType = typeof(Column<>).MakeGenericType(column.SystemType);
            var col = (ColumnBase?)Activator.CreateInstance(SQLColType, column, default, default) ?? throw new ArgumentException($"Unable to create a {SQLColType.Name} from {column.SystemType.Name}.");

            return new ViewParm(col, column_mapping, compound_group, compound_position, compound_key, browsing_key);
        }

        /// <summary>
        /// Converts columns to a dictionary of name and value pairs.
        /// </summary>
        /// <param name="cols">Source columns.</param>
        /// <param name="exclude_colnames">Optional set of column names to exclude.</param>
        /// <returns>Dictionary of column values.</returns>
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

        /// <summary>
        /// Creates a dictionary keyed by column name from the provided columns.
        /// </summary>
        /// <param name="cols">Source columns.</param>
        /// <param name="exclude_colnames">Optional set of column names to exclude.</param>
        /// <returns>Dictionary of columns.</returns>
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


        /// <summary>
        /// Attempts to convert a string value into the column's data type.
        /// </summary>
        /// <param name="col">Target column.</param>
        /// <param name="value_to_convert">String value to convert.</param>
        /// <param name="converted_value">Resulting value if conversion succeeds.</param>
        /// <returns><c>true</c> if conversion was successful.</returns>
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


        /// <summary>
        /// Attempts to convert a <see cref="JsonElement"/> to the specified type.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="source">JSON element to convert.</param>
        /// <param name="result">Converted result when successful.</param>
        /// <returns><c>true</c> if conversion succeeded.</returns>
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

        /// <summary>
        /// Attempts to convert a <see cref="JsonElement"/> into a value compatible with the specified column.
        /// </summary>
        /// <param name="element">JSON element to convert.</param>
        /// <param name="col">Target column definition.</param>
        /// <param name="converted_value">Resulting value when successful.</param>
        /// <returns><c>true</c> if conversion succeeded.</returns>
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
