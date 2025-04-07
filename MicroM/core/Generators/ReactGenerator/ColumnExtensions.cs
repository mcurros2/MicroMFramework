using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.Extensions;
using System.Data;
using System.Globalization;
using System.Text;
using static MicroM.Generators.Constants;

namespace MicroM.Generators.ReactGenerator
{
    public static class ColumnExtensions
    {
        public static string AsDisplayName(this ColumnBase column)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(column.Name.StripColumnPrefix().Replace('_', ' '));
        }

        internal static string ToTypeScriptFlags(this ColumnFlags flags)
        {
            if (flags == ColumnFlags.None) return "EntityColumnFlags.None";
            if (flags.HasAllFlags(ColumnFlags.PK | ColumnFlags.Autonum)) return "c.PKAutonum";
            if (flags.HasFlag(ColumnFlags.PK)) return "c.PK";
            if (flags.HasFlag(ColumnFlags.FK)) return "c.FK";
            if (flags.HasAllFlags(ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Get | ColumnFlags.Delete)) return "c.Edit";
            if (flags.HasAllFlags(ColumnFlags.Insert | ColumnFlags.Update)) return "c.Edit";

            StringBuilder tsFlags = new();

            if (flags.HasFlag(ColumnFlags.Get))
            {
                tsFlags.Append("EntityColumnFlags.get | ");
            }
            if (flags.HasFlag(ColumnFlags.Insert))
            {
                tsFlags.Append("EntityColumnFlags.add | ");
            }
            if (flags.HasFlag(ColumnFlags.Update))
            {
                tsFlags.Append("EntityColumnFlags.edit | ");
            }
            if (flags.HasFlag(ColumnFlags.Delete))
            {
                tsFlags.Append("EntityColumnFlags.delete | ");
            }
            if (flags.HasFlag(ColumnFlags.PK))
            {
                tsFlags.Append("EntityColumnFlags.pk | ");
            }
            if (flags.HasFlag(ColumnFlags.FK))
            {
                tsFlags.Append("EntityColumnFlags.fk | ");
            }
            if (flags.HasFlag(ColumnFlags.Autonum))
            {
                tsFlags.Append("EntityColumnFlags.autoNum | ");
            }

            // MMC: We ignore fakes as for the react client has no use
            //if (flags.HasFlag(ColumnFlags.Fake))
            //{
            //    tsFlags.Append("EntityColumnFlags.fake | ");
            //}

            // Remove the last " | " if there's at least one flag
            if (tsFlags.Length > 0)
            {
                tsFlags.Length -= 3;
            }

            return tsFlags.ToString();
        }


        private static string AsColumnDefinition<T>(this T col) where T : ColumnBase
        {
            SQLServerMetadata m = col.SQLMetadata;
            string length = $"{((col.SQLMetadata.Size > 0) ? $", length: {col.SQLMetadata.Size}" : "")}";
            string scale = $"{((col.SQLMetadata.Scale > 0) ? $", scale: {col.SQLMetadata.Scale}" : "")}";
            string sql_type = $"'{m.SQLType.ToString().ToLowerInvariant()}'";
            string nullable = (m.Nullable ? " | EntityColumnFlags.nullable" : "");
            string flags = col.ColumnMetadata.ToTypeScriptFlags();
            string isArray = col.SQLMetadata.IsArray ? ", isArray: true" : "";
            if (m.SQLType.IsIn(SqlDbType.NChar, SqlDbType.Char, SqlDbType.NVarChar, SqlDbType.VarChar, SqlDbType.Text, SqlDbType.NText))
            {
                return $"{col.Name}: new EntityColumn<{(col.SQLMetadata.IsArray ? "string[]" : "string")}>({{ name: '{col.Name}', type: {sql_type}{length}, flags: {flags}{nullable}, prompt: '{col.AsDisplayName()}'{isArray} }})";
            }
            if (m.SQLType.IsIn(SqlDbType.Decimal, SqlDbType.Int, SqlDbType.BigInt, SqlDbType.SmallInt, SqlDbType.TinyInt, SqlDbType.Money, SqlDbType.Float, SqlDbType.SmallMoney, SqlDbType.Real))
            {
                return $"{col.Name}: new EntityColumn<number>({{ name: '{col.Name}', type: {sql_type}{length}{scale}, flags: {flags}{nullable}, prompt: '{col.AsDisplayName()}' }})";
            }
            if (m.SQLType == SqlDbType.Bit)
            {
                return $"{col.Name}: new EntityColumn<boolean>({{ name: '{col.Name}', type: {sql_type}, flags: {flags}{nullable}, prompt: '{col.AsDisplayName()}' }})";
            }
            if (m.SQLType == SqlDbType.Time)
            {
                return $"{col.Name}: new EntityColumn<string>({{ name: '{col.Name}', type: {sql_type}, flags: {flags}{nullable}, prompt: '{col.AsDisplayName()}' }})";
            }
            if (m.SQLType.IsIn(SqlDbType.Date, SqlDbType.DateTime, SqlDbType.DateTime2, SqlDbType.SmallDateTime))
            {
                return $"{col.Name}: new EntityColumn<Date>({{ name: '{col.Name}', type: {sql_type}, flags: {flags}{nullable}, prompt: '{col.AsDisplayName()}' }})";
            }

            return $"//ERROR: can't translate column definition for {col.Name}. Reason: Unsupported type {m.SQLType}";
        }

        internal static string AsTypeScriptColumnsDefinition<T>(this T cols, string separator = $",\n{TAB}{TAB}") where T : IReadonlyOrderedDictionary<ColumnBase>
        {
            IEnumerator<ColumnBase> col_enumerator = cols.GetWithFlags(ColumnFlags.All, exclude_flags: ColumnFlags.None, exclude_names: SystemColumnNames.AsStringArray).GetEnumerator();
            StringBuilder colsBuilder = new();

            if (col_enumerator.MoveNext())
            {
                colsBuilder.Append(col_enumerator.Current.AsColumnDefinition());

                while (col_enumerator.MoveNext())
                {
                    colsBuilder.Append(separator);
                    colsBuilder.Append(col_enumerator.Current.AsColumnDefinition());
                }
            }

            return colsBuilder.ToString();
        }

        internal static string AsFieldsImport<T>(this T cols) where T : IReadonlyOrderedDictionary<ColumnBase>
        {
            if (cols.Count == 0) return "";

            StringBuilder fieldsImport = new();
            var filtered_cols = cols.GetWithFlags(ColumnFlags.All, exclude_flags: ColumnFlags.None, exclude_names: SystemColumnNames.AsStringArray);

            // If filtered_cols contain a related category and isArray add LookupMultiSelect
            if (filtered_cols.Values.Any((ColumnBase c) => c.SQLMetadata.IsArray && !string.IsNullOrEmpty(c.RelatedCategoryID)))
            {
                fieldsImport.Append("LookupMultiSelect, ");
            }

            // If filtered_cols contain a related category and isArray is false add LookupSelect
            if (filtered_cols.Values.Any((ColumnBase c) => !c.SQLMetadata.IsArray && !string.IsNullOrEmpty(c.RelatedCategoryID)))
            {
                fieldsImport.Append("LookupSelect, ");
            }

            // If filtered_cols contain a char type add TextField
            if (filtered_cols.Values.Any((ColumnBase c) => c.SQLMetadata.SQLType == SqlDbType.NChar || c.SQLMetadata.SQLType == SqlDbType.Char || c.SQLMetadata.SQLType == SqlDbType.NVarChar || c.SQLMetadata.SQLType == SqlDbType.VarChar))
            {
                fieldsImport.Append("TextField, ");
            }

            // If filtered_cols contain a char type and size is >= 255 add TextAreaField
            if (filtered_cols.Values.Any((ColumnBase c) => c.SQLMetadata.SQLType == SqlDbType.NChar || c.SQLMetadata.SQLType == SqlDbType.Char || c.SQLMetadata.SQLType == SqlDbType.NVarChar || c.SQLMetadata.SQLType == SqlDbType.VarChar && c.SQLMetadata.Size >= 255 || c.SQLMetadata.SQLType == SqlDbType.Text || c.SQLMetadata.SQLType == SqlDbType.NText))
            {
                fieldsImport.Append("TextAreaField, ");
            }

            // If filtered_cols contain a date type add DateInputField
            if (filtered_cols.Values.Any((ColumnBase c) => c.SQLMetadata.SQLType == SqlDbType.Date || c.SQLMetadata.SQLType == SqlDbType.DateTime || c.SQLMetadata.SQLType == SqlDbType.DateTime2 || c.SQLMetadata.SQLType == SqlDbType.SmallDateTime))
            {
                fieldsImport.Append("DateInputField, ");
            }

            // If filtered_cols contain a bit type add CheckboxField
            if (filtered_cols.Values.Any((ColumnBase c) => c.SQLMetadata.SQLType == SqlDbType.Bit))
            {
                fieldsImport.Append("CheckboxField, ");
            }

            // If filtered_cols contain a number type add NumberField
            if (filtered_cols.Values.Any((ColumnBase c) => c.SQLMetadata.SQLType == SqlDbType.Decimal || c.SQLMetadata.SQLType == SqlDbType.Int || c.SQLMetadata.SQLType == SqlDbType.BigInt || c.SQLMetadata.SQLType == SqlDbType.SmallInt || c.SQLMetadata.SQLType == SqlDbType.TinyInt || c.SQLMetadata.SQLType == SqlDbType.Money || c.SQLMetadata.SQLType == SqlDbType.Float || c.SQLMetadata.SQLType == SqlDbType.SmallMoney || c.SQLMetadata.SQLType == SqlDbType.Real))
            {
                fieldsImport.Append("NumberField, ");
            }

            // If filtered_cols contain a time type add TimeInputField
            if (filtered_cols.Values.Any((ColumnBase c) => c.SQLMetadata.SQLType == SqlDbType.Time))
            {
                fieldsImport.Append("TimeInputField, ");
            }

            return fieldsImport.ToString().TrimEnd(',', ' ');
        }

        internal static string AsLookupSelect<T>(this T column, string separator = $"\n{TAB}{TAB}{TAB}{TAB}") where T : ColumnBase
        {
            string sep = $"{separator}{TAB}";
            if (!column.SQLMetadata.IsArray)
            {
                return $"{separator}<LookupSelect{sep}entityForm={{formAPI}}{sep}column={{entity.def.columns.{column.Name}}}{sep}entity={{entity}}{sep}lookupDefName={{entity.def.lookups.{column.RelatedCategoryID}.name}}{sep}formStatus={{status}}{sep}enableEdit={{false}}{sep}includeKeyInDescription={{false}}{separator}/>";
            }
            else
            {
                return $"{separator}<LookupMultiSelect{sep}entityForm={{formAPI}}{sep}column={{entity.def.columns.{column.Name}}}{sep}entity={{entity}}{sep}lookupDefName={{entity.def.lookups.{column.RelatedCategoryID}.name}}{sep}formStatus={{status}}{sep}enableEdit={{false}}{sep}searchable={{true}}{sep}includeKeyInDescription={{false}}{sep}creatable={{true}}{sep}createLabel=\"+ Create {column.RelatedCategoryID.AddSpacesAndLowercaseShortWords()}: \"{separator}/>";

            }
        }

        internal static string AsFormField<T>(this T column, bool autoFocus = false, string separator = "\n                ") where T : ColumnBase
        {
            bool autoNum = column.ColumnMetadata.HasFlag(ColumnFlags.Autonum);
            var columnSize = column.SQLMetadata.Size;
            var autofocusParm = $"{(autoFocus ? " autoFocus={{true}}" : "")}";

            switch (column.SQLMetadata.SQLType)
            {
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                case SqlDbType.Text:
                case SqlDbType.NText:
                case SqlDbType.NChar:
                case SqlDbType.Char:
                    if (autoNum)
                    {
                        return $"{separator}{{formMode != \"add\" &&{separator}<TextField entityForm={{formAPI}} column={{entity.def.columns.{column.Name}}} readOnly={{true}} required={{false}} maw=\"20rem\" />{separator}}}";
                    }
                    else if (!string.IsNullOrEmpty(column.RelatedCategoryID))
                    {
                        return column.AsLookupSelect(separator);
                    }
                    else if (columnSize > 0 && columnSize <= 255)
                    {
                        return $"{separator}<TextField entityForm={{formAPI}} column={{entity.def.columns.{column.Name}}}{autofocusParm}{(columnSize <= 50 ? " maw=\"20rem\"" : "")} />";
                    }
                    else
                    {
                        return $"{separator}<TextAreaField entityForm={{formAPI}} column={{entity.def.columns.{column.Name}}} maxRows={{4}} minRows={{4}}{autofocusParm} />";
                    }

                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.SmallDateTime:
                    return $"{separator}<DateInputField entityForm={{formAPI}} column={{entity.def.columns.{column.Name}}}{autofocusParm} />";

                case SqlDbType.Bit:
                    return $"{separator}<CheckboxField entityForm={{formAPI}} column={{entity.def.columns.{column.Name}}}{autofocusParm} />";

                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.Float:
                case SqlDbType.SmallMoney:
                case SqlDbType.Real:
                case SqlDbType.Int:
                case SqlDbType.BigInt:
                case SqlDbType.SmallInt:
                case SqlDbType.TinyInt:
                    return $"{separator}<NumberField entityForm={{formAPI}} column={{entity.def.columns.{column.Name}}}{autofocusParm} />";

                case SqlDbType.Time:
                    return $"{separator}<TimeInputField entityForm={{formAPI}} column={{entity.def.columns.{column.Name}}}{autofocusParm} />";

                default:
                    return $"{separator}<TextField entityForm={{formAPI}} column={{entity.def.columns.{column.Name}}}{autofocusParm}{(columnSize <= 50 ? " maw=\"20rem\"" : "")}/>";
            }

        }

        internal static string AsTypeScriptFieldsControls<T>(this T cols, string separator = $"\n{TAB}{TAB}{TAB}{TAB}") where T : IReadonlyOrderedDictionary<ColumnBase>
        {
            IEnumerator<ColumnBase> col_enumerator = cols.GetWithFlags(ColumnFlags.All, exclude_flags: ColumnFlags.None, exclude_names: SystemColumnNames.AsStringArray).GetEnumerator();
            StringBuilder colsBuilder = new();

            if (col_enumerator.MoveNext())
            {
                colsBuilder.Append(col_enumerator.Current.AsFormField(autoFocus: true, separator: separator));

                while (col_enumerator.MoveNext())
                {
                    colsBuilder.Append(col_enumerator.Current.AsFormField(separator: separator));
                }
            }

            return colsBuilder.ToString();
        }

    }
}
