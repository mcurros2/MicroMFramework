using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary;
using MicroM.Extensions;
using System.Data;
using System.Globalization;
using System.Text;

namespace MicroM.Generators.SQLGenerator
{
    internal static class ColumnExtensions
    {
        internal static string SQLEscape(this string sql_value)
        {
            if (string.IsNullOrEmpty(sql_value)) return sql_value;
            return sql_value.Replace("'", "''");
        }

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

        internal static string AsProcParms<T>(this IReadonlyOrderedDictionary<T> cols, string separator = ", ") where T : ColumnBase
        {
            return GetProcParms(cols.Values.GetEnumerator(), false, separator);
        }

        internal static string AsProcParmsDeclaration<T>(this IReadonlyOrderedDictionary<T> cols, string separator = ", ") where T : ColumnBase
        {
            return GetProcParms(cols.Values.GetEnumerator(), true, separator);
        }

        internal static string AsProcParmsDeclaration<T>(this Dictionary<string, T> cols, string separator = ", ") where T : ColumnBase
        {
            return GetProcParms(cols.Values.GetEnumerator(), true, separator);
        }

        internal static string AsValidateNotNullOrEmptyParm<T>(this IReadonlyOrderedDictionary<T> cols, string separator = "\n", bool for_i_update = false) where T : ColumnBase
        {
            // $"{separator}{col_enumerator.Current.AsProcParm(include_declaration)}"
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

        internal static string AsColumnValuePairs<T>(this IReadonlyOrderedDictionary<T> cols, string pair_separator = "=", string union_string = " and ", string alias = "") where T : ColumnBase
        {
            return cols.Values.AsColumnValuePairs(pair_separator, union_string, alias);
        }

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

        internal static string AsNullIfChecks<T>(this IReadonlyOrderedDictionary<T> cols, string separator = "\n") where T : ColumnBase
        {
            return AsNullIfChecks(cols.Values.GetEnumerator(), separator);
        }



    }
}
