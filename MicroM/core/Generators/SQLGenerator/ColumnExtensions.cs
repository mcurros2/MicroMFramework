using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.Extensions;
using System.Data;
using System.Globalization;
using System.Text;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Provides helper methods for converting <see cref="ColumnBase"/> definitions into
    /// fragments of SQL used throughout the generator.
    /// </summary>
    internal static class ColumnExtensions
    {
        /// <summary>
        /// Escapes single quotes in a string so it can be safely embedded in SQL statements.
        /// </summary>
        /// <param name="sql_value">Value to escape.</param>
        /// <returns>The escaped value or the original string if it was <c>null</c> or empty.</returns>
        internal static string SQLEscape(this string sql_value)
        {
            if (string.IsNullOrEmpty(sql_value)) return sql_value;
            return sql_value.Replace("'", "''");
        }

        /// <summary>
        /// Converts a PascalCase class name into a snake_case SQL identifier and optionally adds an alias.
        /// </summary>
        /// <param name="class_name">The original class name.</param>
        /// <param name="alias">Optional alias appended after the identifier.</param>
        /// <returns>A SQL friendly name.</returns>
        internal static string ToSQLName(this string class_name, string alias = "")
        {
            StringBuilder ret = new(class_name.Length + 20);

            using (var str_enum = class_name.GetEnumerator())
            {
                if (str_enum.MoveNext())
                {
                    char c = str_enum.Current;
                    ret.Append(char.ToLowerInvariant(c));
                    while (str_enum.MoveNext())
                    {
                        c = str_enum.Current;
                        if (char.IsUpper(c)) ret.Append('_');
                        ret.Append(char.ToLowerInvariant(c));

                    }

                }

            }
            if (!string.IsNullOrEmpty(alias)) ret.AppendFormat(CultureInfo.InvariantCulture, " {0}", alias);
            return ret.ToString();
        }

        /// <summary>
        /// Returns the first column that appears to contain descriptive text.
        /// </summary>
        /// <param name="cols">Columns to inspect.</param>
        /// <param name="alias">Optional table alias used in the returned expression.</param>
        /// <returns>A column reference suitable for use in SQL or an empty string if none is found.</returns>
        internal static string GetDescriptionColumn(this IReadonlyOrderedDictionary<ColumnBase> cols, string alias = "")
        {
            foreach (var col in cols)
            {
                if (col.SQLMetadata.SQLType.IsIn(SqlDbType.VarChar, SqlDbType.NVarChar, SqlDbType.Text, SqlDbType.NText) && (col.SQLMetadata.Size == 0 || col.SQLMetadata.Size >= 80) && col.ColumnMetadata.HasFlag(ColumnFlags.Fake) == false)
                {
                    return $"{(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col.Name}"; 
                }
            }
            return "";
        }

        /// <summary>
        /// Builds a SQL type declaration for the provided column.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="col">Column definition to convert.</param>
        /// <param name="include_nullable">When true, includes <c>NOT NULL</c> for non-nullable columns.</param>
        /// <returns>SQL type string.</returns>
        internal static string AsSQLTypeString<T>(this T col, bool include_nullable = true) where T : ColumnBase
        {
            SQLServerMetadata m = col.SQLMetadata;
            if (m.SQLType.IsIn(SqlDbType.NChar, SqlDbType.Char, SqlDbType.NVarChar, SqlDbType.VarChar, SqlDbType.Binary, SqlDbType.VarBinary))
            {
                return $"{m.SQLType}({(m.Size == 0 ? "max" : m.Size)}){(include_nullable ? m.Nullable ? "" : " NOT NULL" : "")}";
            }
            if (m.SQLType == SqlDbType.Decimal)
            {
                return $"{m.SQLType}({m.Precision}, {m.Scale}){(include_nullable ? m.Nullable ? "" : " NOT NULL" : "")}";
            }

            return $"{m.SQLType}{(include_nullable ? m.Nullable ? "" : " NOT NULL" : "")}";
        }

        /// <summary>
        /// Formats a stored procedure parameter for the column.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="col">Column to convert into a parameter.</param>
        /// <param name="include_declaration">If true, includes the SQL type declaration.</param>
        /// <returns>The formatted parameter string.</returns>
        internal static string AsProcParm<T>(this T col, bool include_declaration) where T : ColumnBase
        {
            return $"@{col.SQLParameterName}{(include_declaration ? $" {col.AsSQLTypeString(false)}" : "")}";
        }

        private static string GetProcParms<T>(IEnumerator<T> col_enumerator, bool include_declaration, string separator = ", ") where T : ColumnBase
        {
            string col_parms = "";
            if (col_enumerator.MoveNext())
            {
                col_parms += col_enumerator.Current.AsProcParm(include_declaration);
                while (col_enumerator.MoveNext())
                {
                    col_parms += $"{separator}{col_enumerator.Current.AsProcParm(include_declaration)}";
                }
            }
            return col_parms;
        }

        private static void AppendProcColumn(StringBuilder sb, ColumnBase col, string alias, string cat_alias, string separator, bool rtrim_chars)
        {
            if (col.ColumnMetadata.HasFlag(ColumnFlags.Fake))
            {
                bool is_category = !string.IsNullOrEmpty(col.RelatedCategoryID);
                bool is_status = !string.IsNullOrEmpty(col.RelatedStatusID);
                if (is_category == false && is_status == false)
                {
                    sb.Append(col.SQLMetadata.IsArray ? $"{separator}'[]' /* fake column {col.Name} */" : $"{separator}'' /* fake column {col.Name} */");
                }
                else
                {
                    if (col.SQLMetadata.IsArray == false)
                    {
                        string col_name = $"{cat_alias}.{(is_category ? nameof(CategoriesValuesDef.c_categoryvalue_id) : nameof(StatusValuesDef.c_statusvalue_id))}";
                        if (rtrim_chars && col.SQLMetadata.SQLType.IsIn(SqlDbType.Char, SqlDbType.NChar))
                        {
                            sb.Append(CultureInfo.InvariantCulture, $"{separator}[{col.Name}] = rtrim({col_name})");
                        }
                        else
                        {
                            sb.Append(CultureInfo.InvariantCulture, $"{separator}{col_name}");
                        }
                    }
                    else
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"{separator}{col.AsProcParm(false)}");
                    }
                }
            }
            else
            {
                if (rtrim_chars && col.SQLMetadata.SQLType.IsIn(SqlDbType.Char, SqlDbType.NChar))
                {
                    sb.Append(CultureInfo.InvariantCulture, $"{separator}[{col.Name}] = rtrim({(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col.Name})");
                }
                else
                {
                    sb.Append(CultureInfo.InvariantCulture, $"{separator}{(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col.Name}");
                }
            }

        }

        /// <summary>
        /// Builds a separated list of column expressions for stored procedures.
        /// </summary>
        /// <param name="cols">Columns to process.</param>
        /// <param name="separator">Separator placed between columns.</param>
        /// <param name="alias">Optional table alias used for real columns.</param>
        /// <param name="cat_alias">Starting alias for category or status tables.</param>
        /// <param name="rtrim_chars">Whether to trim fixed width character columns.</param>
        /// <returns>Formatted column list.</returns>
        internal static string AsProcColumns(this IReadonlyOrderedDictionary<ColumnBase> cols, string separator = ", ", string alias = "", string cat_alias = "b", bool rtrim_chars = true)
        {
            StringBuilder sb = new();
            using var col_enumerator = cols.Values.GetEnumerator();
            if (col_enumerator.MoveNext())
            {
                var col = col_enumerator.Current;
                AppendProcColumn(sb, col, alias, cat_alias, "", rtrim_chars);
                if (col.ColumnMetadata.HasFlag(ColumnFlags.Fake) && (!string.IsNullOrEmpty(col.RelatedCategoryID) || !string.IsNullOrEmpty(col.RelatedStatusID)) && col.SQLMetadata.IsArray == false)
                {
                    cat_alias = ((char)(cat_alias[0] + 1)).ToString();
                }

                while (col_enumerator.MoveNext())
                {
                    col = col_enumerator.Current;
                    AppendProcColumn(sb, col, alias, cat_alias, separator, rtrim_chars);
                    if (col.ColumnMetadata.HasFlag(ColumnFlags.Fake) && (!string.IsNullOrEmpty(col.RelatedCategoryID) || !string.IsNullOrEmpty(col.RelatedStatusID)) && col.SQLMetadata.IsArray == false)
                    {
                        cat_alias = ((char)(cat_alias[0] + 1)).ToString();
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Formats the columns as parameters for a stored procedure call.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="cols">Columns to convert.</param>
        /// <param name="separator">Separator placed between parameters.</param>
        /// <returns>Comma separated parameter list.</returns>
        internal static string AsProcParms<T>(this IReadonlyOrderedDictionary<T> cols, string separator = ", ") where T : ColumnBase
        {
            return GetProcParms(cols.Values.GetEnumerator(), false, separator);
        }

        /// <summary>
        /// Formats the columns as parameter declarations for a stored procedure.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="cols">Columns to convert.</param>
        /// <param name="separator">Separator placed between parameters.</param>
        /// <returns>Parameter declaration list.</returns>
        internal static string AsProcParmsDeclaration<T>(this IReadonlyOrderedDictionary<T> cols, string separator = ", ") where T : ColumnBase
        {
            return GetProcParms(cols.Values.GetEnumerator(), true, separator);
        }

        /// <summary>
        /// Formats dictionary based column collections as parameter declarations for a stored procedure.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="cols">Dictionary of columns to convert.</param>
        /// <param name="separator">Separator placed between parameters.</param>
        /// <returns>Parameter declaration list.</returns>
        internal static string AsProcParmsDeclaration<T>(this Dictionary<string, T> cols, string separator = ", ") where T : ColumnBase
        {
            return GetProcParms(cols.Values.GetEnumerator(), true, separator);
        }

        /// <summary>
        /// Generates T-SQL checks ensuring parameters are not null or empty.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="cols">Columns to validate.</param>
        /// <param name="separator">Separator inserted before each check.</param>
        /// <param name="for_i_update">When true, prefixes error assignments for insert/update procedures.</param>
        /// <returns>Validation statements.</returns>
        internal static string AsValidateNotNullOrEmptyParm<T>(this IReadonlyOrderedDictionary<T> cols, string separator = "\n", bool for_i_update = false) where T : ColumnBase
        {
            string result = "";
            using var col_enumerator = cols.Values.GetEnumerator();
            if (col_enumerator.MoveNext())
            {
                var col = col_enumerator.Current;
                var col_parm = col_enumerator.Current.AsProcParm(false);
                string trim_char = "";
                if (!col.ColumnMetadata.HasFlag(ColumnFlags.Autonum) && !col.SQLMetadata.Nullable)
                {
                    if (col.SQLMetadata.SQLType.IsIn(SqlDbType.Char, SqlDbType.VarChar, SqlDbType.NChar, SqlDbType.NVarChar))
                        trim_char = $" or trim({col_parm}) = ''";

                    result += $"{separator}if ({col_parm} is null{trim_char}) begin select {(for_i_update ? "@errn=" : "")}11, {(for_i_update ? "@msg=" : "")}'The parameter {col_parm} cannot be null or empty' return end";
                }

                while (col_enumerator.MoveNext())
                {
                    col = col_enumerator.Current;
                    col_parm = col_enumerator.Current.AsProcParm(false);
                    trim_char = "";
                    if (!col.ColumnMetadata.HasFlag(ColumnFlags.Autonum) && !col.SQLMetadata.Nullable)
                    {
                        if (col.SQLMetadata.SQLType.IsIn(SqlDbType.Char, SqlDbType.VarChar, SqlDbType.NChar, SqlDbType.NVarChar))
                            trim_char = $" or trim({col_parm}) = ''";

                        result += $"{separator}if ({col_parm} is null{trim_char}) begin select {(for_i_update ? "@errn=" : "")}11, {(for_i_update ? "@msg=" : "")}'The parameter {col_parm} cannot be null or empty' return end";
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Formats the columns as <c>column = @parameter</c> pairs.
        /// </summary>
        /// <param name="cols">Columns to convert.</param>
        /// <param name="pair_separator">Operator between column and parameter.</param>
        /// <param name="union_string">String used to join pairs.</param>
        /// <param name="alias">Optional table alias.</param>
        /// <returns>Concatenated column/value expressions.</returns>
        internal static string AsColumnValuePairs(this IEnumerable<ColumnBase> cols, string pair_separator = "=", string union_string = " and ", string alias = "")
        {
            using var col_enumerator = cols.GetEnumerator();
            string col_parms = "";
            if (col_enumerator.MoveNext())
            {
                col_parms += $"{(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col_enumerator.Current.Name} {pair_separator} @{col_enumerator.Current.SQLParameterName}";
                while (col_enumerator.MoveNext()) col_parms += $"{union_string}{(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col_enumerator.Current.Name} {pair_separator} @{col_enumerator.Current.SQLParameterName}";
            }
            return col_parms;
        }

        /// <summary>
        /// Formats the columns as <c>column = @parameter</c> pairs using an ordered dictionary.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="cols">Columns to convert.</param>
        /// <param name="pair_separator">Operator between column and parameter.</param>
        /// <param name="union_string">String used to join pairs.</param>
        /// <param name="alias">Optional table alias.</param>
        /// <returns>Concatenated column/value expressions.</returns>
        internal static string AsColumnValuePairs<T>(this IReadonlyOrderedDictionary<T> cols, string pair_separator = "=", string union_string = " and ", string alias = "") where T : ColumnBase
        {
            return cols.Values.AsColumnValuePairs(pair_separator, union_string, alias);
        }

        /// <summary>
        /// Formats the columns as <c>column LIKE phrase</c> pairs joined with a union string.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="cols">Columns to convert.</param>
        /// <param name="pair_separator">The comparison operator (typically <c>LIKE</c>).</param>
        /// <param name="union_string">String used to join pairs.</param>
        /// <param name="alias">Optional table alias.</param>
        /// <returns>Concatenated like expressions.</returns>
        internal static string AsLikeValuePairs<T>(this IReadonlyOrderedDictionary<T> cols, string pair_separator = "like", string union_string = " or ", string alias = "") where T : ColumnBase
        {
            using var col_enumerator = cols.Values.GetEnumerator();
            string col_parms = "";
            if (col_enumerator.MoveNext())
            {
                col_parms += $"isnull(rtrim({(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col_enumerator.Current.Name}),'') {pair_separator} l.phrase";
                while (col_enumerator.MoveNext()) col_parms += $"{union_string}isnull(rtrim({(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col_enumerator.Current.Name}),'') {pair_separator} l.phrase";
            }
            return col_parms;

        }

        /// <summary>
        /// Formats the columns as <c>TitleCase Column = value</c> pairs for use in SQL titles.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="cols">Columns to convert.</param>
        /// <param name="pair_separator">Operator between column and value.</param>
        /// <param name="union_string">String used to join pairs.</param>
        /// <param name="alias">Optional table alias.</param>
        /// <param name="trim_char_cols">Whether to trim fixed width character columns.</param>
        /// <returns>Concatenated title/value expressions.</returns>
        internal static string AsTitleColumnPairs<T>(this IReadonlyOrderedDictionary<T> cols, string pair_separator = "=", string union_string = ", ", string alias = "", bool trim_char_cols = true) where T : ColumnBase
        {
            using var col_enumerator = cols.Values.GetEnumerator();
            string col_parms = ""; var ti = System.Globalization.CultureInfo.InvariantCulture.TextInfo;
            if (col_enumerator.MoveNext())
            {
                string col_name = col_enumerator.Current.Name;
                SqlDbType col_type = col_enumerator.Current.SQLMetadata.SQLType;
                col_parms += $"[{ti.ToTitleCase(col_name.StripColumnPrefix().Replace('_', ' '))}] {pair_separator} {(trim_char_cols && col_type.IsIn(SqlDbType.Char, SqlDbType.NChar) ? $"rtrim({(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col_name})" : $"{(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col_name}")}";
                while (col_enumerator.MoveNext())
                {
                    col_name = col_enumerator.Current.Name;
                    col_type = col_enumerator.Current.SQLMetadata.SQLType;
                    col_parms += $"{union_string}[{ti.ToTitleCase(col_name.StripColumnPrefix().Replace('_', ' '))}] {pair_separator} {(trim_char_cols && col_type.IsIn(SqlDbType.Char, SqlDbType.NChar) ? $"rtrim({(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col_name})" : $"{(!string.IsNullOrEmpty(alias) ? $"{alias}." : "")}{col_name}")}";
                }
            }
            return col_parms;

        }

        private static string AsNullIfChecks<T>(IEnumerator<T> col_enumerator, string separator = "\n") where T : ColumnBase
        {
            var sb = new StringBuilder();
            if (col_enumerator.MoveNext())
            {
                string parm = col_enumerator.Current.AsProcParm(false);

                if (col_enumerator.Current.SQLMetadata.Nullable && col_enumerator.Current.SQLMetadata.SQLType.IsIn(SqlDbType.Char, SqlDbType.NChar, SqlDbType.VarChar, SqlDbType.NVarChar, SqlDbType.Text, SqlDbType.NText)) sb.Append(CultureInfo.InvariantCulture, $"{separator}set {parm} = NULLIF({parm},'')");
                while (col_enumerator.MoveNext())
                {
                    parm = col_enumerator.Current.AsProcParm(false);
                    if (col_enumerator.Current.SQLMetadata.Nullable && col_enumerator.Current.SQLMetadata.SQLType.IsIn(SqlDbType.Char, SqlDbType.NChar, SqlDbType.VarChar, SqlDbType.NVarChar, SqlDbType.Text, SqlDbType.NText)) sb.Append(CultureInfo.InvariantCulture, $"{separator}set {parm} = NULLIF({parm},'')");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates <c>NULLIF</c> checks for nullable string columns.
        /// </summary>
        /// <typeparam name="T">Concrete column type.</typeparam>
        /// <param name="cols">Columns to inspect.</param>
        /// <param name="separator">Separator inserted before each statement.</param>
        /// <returns>Concatenated <c>NULLIF</c> statements.</returns>
        internal static string AsNullIfChecks<T>(this IReadonlyOrderedDictionary<T> cols, string separator = "\n") where T : ColumnBase
        {
            return AsNullIfChecks(cols.Values.GetEnumerator(), separator);
        }



    }
}
