using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Excel;
using MicroM.Extensions;
using MicroM.Web.Services;
using Sylvan.Data.Excel;
using System.Data;
using System.Globalization;
using System.Text;

namespace MicroM.ImportData;

public static class EntityImportData
{
    internal sealed record ResolvedImportDataMapping(int SourceIndex, string DestinationColumnName);

    public static void MapCSVDataToEntity<T>(this T entity, Dictionary<string, string> data) where T : EntityBase
    {
        foreach (var kvp in data)
        {
            if (entity.Def.Columns.Contains(kvp.Key))
            {
                var value = kvp.Value;
                var col = entity.Def.Columns[kvp.Key];

                if (col == null) continue;

                // skip columns that are not insertable
                if (!col.ColumnMetadata.HasFlag(ColumnFlags.Insert)) continue;

                // skip system columns
                if (col.Name.IsIn(SystemColumnNames.AsStringArray) || col.ColumnMetadata.HasFlag(ColumnFlags.APIReadOnly)) continue;

                if (!string.IsNullOrEmpty(value))
                {
                    if (col.SQLMetadata.SQLType.IsIn(SqlDbType.VarChar, SqlDbType.NVarChar, SqlDbType.Char, SqlDbType.NChar, SqlDbType.Text, SqlDbType.NText))
                    {
                        col.ValueObject = value;
                    }
                    else if (col.SQLMetadata.SQLType == SqlDbType.Bit)
                    {
                        col.ValueObject = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (col.SQLMetadata.SQLType.IsIn(SqlDbType.Int, SqlDbType.BigInt, SqlDbType.SmallInt, SqlDbType.TinyInt))
                    {
                        col.ValueObject = int.Parse(value, CultureInfo.InvariantCulture);
                    }
                    else if (col.SQLMetadata.SQLType.IsIn(SqlDbType.Decimal, SqlDbType.Money, SqlDbType.SmallMoney))
                    {
                        col.ValueObject = decimal.Parse(value, CultureInfo.InvariantCulture);
                    }
                    else if (col.SQLMetadata.SQLType.IsIn(SqlDbType.Float, SqlDbType.Real))
                    {
                        col.ValueObject = double.Parse(value, CultureInfo.InvariantCulture);
                    }
                    else if (col.SQLMetadata.SQLType.IsIn(SqlDbType.DateTime, SqlDbType.DateTime2, SqlDbType.Date, SqlDbType.Time, SqlDbType.DateTimeOffset, SqlDbType.SmallDateTime))
                    {
                        col.ValueObject = DateTime.Parse(value, CultureInfo.InvariantCulture);
                    }
                    else if (col.SQLMetadata.SQLType == SqlDbType.UniqueIdentifier)
                    {
                        col.ValueObject = Guid.Parse(value, CultureInfo.InvariantCulture);
                    }
                    else if (col.SQLMetadata.SQLType.IsIn(SqlDbType.Binary, SqlDbType.VarBinary, SqlDbType.Image))
                    {
                        col.ValueObject = Encoding.UTF8.GetBytes(value);
                    }
                }
                else
                {
                    col.ValueObject = null;
                }
            }

        }
    }

    public static async Task<CSVImportResult> ImportDataFromCSV<T>(this T entity, List<Dictionary<string, string>> data, MicroMOptions options, Dictionary<string, object>? claims, IWebAPIServices api, string app_id, Dictionary<string, object>? parentKeys, CancellationToken ct) where T : EntityBase
    {

        CSVImportResult result = new();
        if (data.Count == 0)
        {
            return result;
        }

        var client = entity.Client;

        try
        {
            await client.Connect(ct);

            foreach (var row in data)
            {
                try
                {
                    entity.MapCSVDataToEntity(row);

                    // Override application keys
                    entity.SetColumnValues(api.entitiesService.GetApplicationKeys(app_id));

                    // Override the parentkeys
                    if (parentKeys != null && parentKeys.Count > 0)
                    {
                        entity.SetKeyValues(parentKeys);
                    }

                    var insert_result = await entity.InsertData(ct, options: options, server_claims: claims, api: api, app_id: app_id);
                    if (insert_result.Failed)
                    {
                        result.ErrorCount++;
                        result.Errors.Add(result.ProcessedCount + 1, insert_result.Results?[0].Message ?? "Unknown error");
                    }
                    else
                    {
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add(result.ProcessedCount + 1, ex.Message);
                }
                finally
                {
                    result.ProcessedCount++;
                }
            }
        }
        finally
        {
            await client.Disconnect();
        }

        return result;
    }

    public static async Task<CSVImportResult> ImportDataFromExcel<T>(
        this T entity,
        Stream excelStream,
        string? sheetName,
        int? initialRow,
        MicroMOptions options,
        Dictionary<string, object>? claims,
        IWebAPIServices api,
        string app_id,
        Dictionary<string, object>? parentKeys,
        CancellationToken ct) where T : EntityBase
    {
        return await entity.ImportDataFromExcel(
            excelStream,
            ExcelWorkbookType.ExcelXml,
            new ExcelImportMapping { SheetName = sheetName },
            initialRow,
            options,
            claims,
            api,
            app_id,
            parentKeys,
            ct);
    }

    public static async Task<CSVImportResult> ImportDataFromExcel<T>(
        this T entity,
        Stream excelStream,
        ExcelWorkbookType workbookType,
        ExcelImportMapping? importMapping,
        int? initialRow,
        MicroMOptions options,
        Dictionary<string, object>? claims,
        IWebAPIServices api,
        string app_id,
        Dictionary<string, object>? parentKeys,
        CancellationToken ct) where T : EntityBase
    {
        CSVImportResult result = new();
        var client = entity.Client;
        var rows = ExcelReader.ReadExcelAsync(excelStream, workbookType, importMapping?.SheetName, initialRow ?? 1, ct);

        await using var rowEnumerator = rows.GetAsyncEnumerator(ct);
        if (!await rowEnumerator.MoveNextAsync())
        {
            throw new InvalidDataException($"The worksheet '{importMapping?.SheetName ?? "(first sheet)"}' does not contain a header row at row {initialRow ?? 1}.");
        }

        object?[] headerRow = rowEnumerator.Current;
        var resolvedMapping = ResolveExcelMapping(entity, headerRow, importMapping);
        bool useExplicitMapping = importMapping?.Mapping?.Length > 0;

        // Preserve the existing Excel import result semantics, which count the header row.
        result.ProcessedCount++;
        int rowIndex = 0;

        try
        {
            await client.Connect(ct);

            while (await rowEnumerator.MoveNextAsync())
            {
                var row = rowEnumerator.Current;
                if (row == null || row.Length == 0)
                {
                    result.ProcessedCount++;
                    continue;
                }

                Dictionary<string, object?> data = new(StringComparer.OrdinalIgnoreCase);
                if (useExplicitMapping)
                {
                    foreach (var mapping in resolvedMapping)
                    {
                        data[mapping.DestinationColumnName] = row[mapping.SourceIndex];
                    }
                }
                else
                {
                    for (int i = 0; i < headerRow.Length && i < row.Length; i++)
                    {
                        var key = headerRow[i]?.ToString() ?? $"Column{i}";
                        data[key] = row[i];
                    }
                }

                try
                {
                    entity.MapExcelDataToEntity(data);

                    entity.SetColumnValues(api.entitiesService.GetApplicationKeys(app_id));

                    if (parentKeys != null && parentKeys.Count > 0) entity.SetKeyValues(parentKeys);

                    var insert_result = await entity.InsertData(ct, options: options, server_claims: claims, api: api, app_id: app_id);

                    if (insert_result.Failed)
                    {
                        result.ErrorCount++;
                        result.Errors.Add(rowIndex + 1, insert_result.Results?[0].Message ?? "Unknown error");
                    }
                    else
                    {
                        result.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add(rowIndex + 1, ex.Message);
                }
                finally
                {
                    result.ProcessedCount++;
                    rowIndex++;
                }
            }
        }
        finally
        {
            await client.Disconnect();
        }

        return result;
    }

    internal static IReadOnlyList<ResolvedImportDataMapping> ResolveExcelMapping<T>(T entity, object?[] headerRow, ExcelImportMapping? importMapping) where T : EntityBase
    {
        if (importMapping?.Mapping == null || importMapping.Mapping.Length == 0) return [];

        List<ResolvedImportDataMapping> result = new(importMapping.Mapping.Length);
        HashSet<string> destinations = new(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in importMapping.Mapping)
        {
            if (string.IsNullOrWhiteSpace(mapping.DestinationColumnName))
            {
                throw new InvalidDataException("An Excel import destination column name cannot be empty.");
            }

            if (!destinations.Add(mapping.DestinationColumnName))
            {
                throw new InvalidDataException($"The destination column '{mapping.DestinationColumnName}' is mapped more than once.");
            }

            if (!entity.Def.Columns.TryGetValue(mapping.DestinationColumnName, out var destinationColumn) || destinationColumn == null)
            {
                throw new InvalidDataException($"The destination column '{mapping.DestinationColumnName}' does not exist in entity '{entity.Def.Name}'.");
            }

            if (!destinationColumn.ColumnMetadata.HasFlag(ColumnFlags.Insert)
                || destinationColumn.ColumnMetadata.HasFlag(ColumnFlags.APIReadOnly)
                || destinationColumn.Name.IsIn(SystemColumnNames.AsStringArray))
            {
                throw new InvalidDataException($"The destination column '{mapping.DestinationColumnName}' cannot be imported.");
            }

            int sourceIndex;
            if (!string.IsNullOrWhiteSpace(mapping.SourceHeader))
            {
                List<int> matchingHeaderIndexes = [];
                for (int i = 0; i < headerRow.Length; i++)
                {
                    if (string.Equals(headerRow[i]?.ToString(), mapping.SourceHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingHeaderIndexes.Add(i);
                    }
                }

                if (matchingHeaderIndexes.Count == 0)
                {
                    throw new InvalidDataException($"The source header '{mapping.SourceHeader}' was not found.");
                }

                if (matchingHeaderIndexes.Count == 1)
                {
                    sourceIndex = matchingHeaderIndexes[0];
                }
                else if (mapping.SourceIndex is int duplicateIndex && matchingHeaderIndexes.Contains(duplicateIndex))
                {
                    sourceIndex = duplicateIndex;
                }
                else
                {
                    throw new InvalidDataException($"The source header '{mapping.SourceHeader}' is duplicated and requires a source index that identifies one of its columns.");
                }
            }
            else
            {
                if (mapping.SourceIndex is not int index)
                {
                    throw new InvalidDataException("An Excel import mapping must specify a source header or source index.");
                }

                if (index < 0 || index >= headerRow.Length)
                {
                    throw new InvalidDataException($"Source index {index} is outside the worksheet header.");
                }

                sourceIndex = index;
            }

            result.Add(new(sourceIndex, destinationColumn.Name));
        }

        return result;
    }

    internal static void MapExcelDataToEntity<T>(this T entity, IReadOnlyDictionary<string, object?> data) where T : EntityBase
    {
        foreach (var item in data)
        {
            if (entity.Def.Columns.TryGetValue(item.Key, out var col) && col != null)
            {
                // skip columns that are not insertable
                if (!col.ColumnMetadata.HasFlag(ColumnFlags.Insert) || col.ColumnMetadata.HasFlag(ColumnFlags.APIReadOnly) || !col.OverrideWith.IsNullOrEmpty()) continue;

                // skip system columns
                if (col.Name.IsIn(SystemColumnNames.AsStringArray)) continue;

                col.ValueObject = ConvertExcelValue(col, item.Value);
            }
        }
    }

    internal static object? ConvertExcelValue(ColumnBase destinationColumn, object? sourceValue)
    {
        if (sourceValue == null || sourceValue == DBNull.Value) return null;

        if (destinationColumn.SQLMetadata.SQLType.IsTypeAccepted(sourceValue.GetType()))
        {
            if (sourceValue is double doubleValue && !double.IsFinite(doubleValue))
            {
                throw InvalidExcelConversion(sourceValue, destinationColumn, "The value must be finite.");
            }

            if (sourceValue is float singleValue && !float.IsFinite(singleValue))
            {
                throw InvalidExcelConversion(sourceValue, destinationColumn, "The value must be finite.");
            }

            return sourceValue;
        }

        return destinationColumn.SQLMetadata.SQLType switch
        {
            SqlDbType.Char or SqlDbType.NChar or SqlDbType.NText or SqlDbType.NVarChar or SqlDbType.Text or SqlDbType.VarChar => ConvertExcelString(sourceValue, destinationColumn),
            SqlDbType.BigInt => ConvertExcelInt64(sourceValue, destinationColumn),
            SqlDbType.Int => ConvertExcelInt32(sourceValue, destinationColumn),
            SqlDbType.SmallInt => ConvertExcelInt16(sourceValue, destinationColumn),
            SqlDbType.TinyInt => ConvertExcelByte(sourceValue, destinationColumn),
            SqlDbType.Decimal or SqlDbType.Money or SqlDbType.SmallMoney => ConvertExcelDecimal(sourceValue, destinationColumn),
            SqlDbType.Float => ConvertExcelDouble(sourceValue, destinationColumn),
            SqlDbType.Real => ConvertExcelSingle(sourceValue, destinationColumn),
            SqlDbType.Bit => ConvertExcelBoolean(sourceValue, destinationColumn),
            SqlDbType.DateTime or SqlDbType.DateTime2 or SqlDbType.SmallDateTime => ConvertExcelDateTime(sourceValue, destinationColumn),
            SqlDbType.Date => ConvertExcelDate(sourceValue, destinationColumn),
            SqlDbType.Time => ConvertExcelTime(sourceValue, destinationColumn),
            SqlDbType.DateTimeOffset => ConvertExcelDateTimeOffset(sourceValue, destinationColumn),
            SqlDbType.UniqueIdentifier => ConvertExcelGuid(sourceValue, destinationColumn),
            _ => throw InvalidExcelConversion(sourceValue, destinationColumn)
        };
    }

    private static readonly string[] ExcelDateFormats =
    [
        "yyyyMMdd",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyy.MM.dd"
    ];

    private static readonly string[] ExcelDateTimeFormats = CreateExcelDateTimeFormats();
    private static readonly string[] ExcelUtcDateTimeFormats = CreateExcelDateTimeFormats("'Z'");
    private static readonly string[] ExcelOffsetDateTimeFormats = CreateExcelDateTimeFormats("zzz");

    private static string[] CreateExcelDateTimeFormats(string suffix = "")
    {
        string[] dateFormats = ["yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd"];
        string[] dateTimeSeparators = ["'T'", " "];
        string[] timeFormats = ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"];
        List<string> formats = new(dateFormats.Length * dateTimeSeparators.Length * timeFormats.Length);

        foreach (var dateFormat in dateFormats)
        {
            foreach (var dateTimeSeparator in dateTimeSeparators)
            {
                foreach (var timeFormat in timeFormats)
                {
                    formats.Add($"{dateFormat}{dateTimeSeparator}{timeFormat}{suffix}");
                }
            }
        }

        return [.. formats];
    }

    private static string ConvertExcelString(object sourceValue, ColumnBase destinationColumn)
    {
        return sourceValue switch
        {
            DateOnly value => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime value => value.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset value => value.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly value => value.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan value => value.ToString("c", CultureInfo.InvariantCulture),
            Guid value => value.ToString("D"),
            bool value => value ? bool.TrueString : bool.FalseString,
            byte value => value.ToString(CultureInfo.InvariantCulture),
            sbyte value => value.ToString(CultureInfo.InvariantCulture),
            short value => value.ToString(CultureInfo.InvariantCulture),
            ushort value => value.ToString(CultureInfo.InvariantCulture),
            int value => value.ToString(CultureInfo.InvariantCulture),
            uint value => value.ToString(CultureInfo.InvariantCulture),
            long value => value.ToString(CultureInfo.InvariantCulture),
            ulong value => value.ToString(CultureInfo.InvariantCulture),
            decimal value => value.ToString(CultureInfo.InvariantCulture),
            double value when double.IsFinite(value) => value.ToString("R", CultureInfo.InvariantCulture),
            float value when float.IsFinite(value) => value.ToString("R", CultureInfo.InvariantCulture),
            char value => value.ToString(),
            _ => throw InvalidExcelConversion(sourceValue, destinationColumn, "Only scalar values can be converted to text.")
        };
    }

    private static DateTime ConvertExcelDateTime(object sourceValue, ColumnBase destinationColumn)
    {
        if (sourceValue is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        if (sourceValue is string text)
        {
            text = text.Trim();
            if (DateTime.TryParseExact(text, ExcelDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                || DateTime.TryParseExact(text, ExcelDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return date;
            }

            if (DateTime.TryParseExact(text, ExcelUtcDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
            {
                return date;
            }
        }

        if (TryConvertExcelDouble(sourceValue, out var serialValue))
        {
            try
            {
                return DateTime.FromOADate(serialValue);
            }
            catch (ArgumentException)
            {
            }
        }

        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected an unambiguous year-first date/date-time or a valid Excel serial date.");
    }

    private static DateOnly ConvertExcelDate(object sourceValue, ColumnBase destinationColumn)
    {
        if (sourceValue is string text
            && DateOnly.TryParseExact(text.Trim(), ExcelDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        if (TryConvertExcelDouble(sourceValue, out var serialValue))
        {
            try
            {
                return DateOnly.FromDateTime(DateTime.FromOADate(serialValue));
            }
            catch (ArgumentException)
            {
            }
        }

        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected yyyyMMdd, a separated year-first date, or a valid Excel serial date.");
    }

    private static TimeOnly ConvertExcelTime(object sourceValue, ColumnBase destinationColumn)
    {
        if (sourceValue is string text
            && TimeOnly.TryParseExact(text.Trim(), ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFFFFFF"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        if (TryConvertExcelDouble(sourceValue, out var serialValue) && serialValue >= 0d && serialValue < 1d)
        {
            try
            {
                return TimeOnly.FromDateTime(DateTime.FromOADate(serialValue));
            }
            catch (ArgumentException)
            {
            }
        }

        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected an invariant 24-hour time or an Excel time fraction between 0 and 1.");
    }

    private static DateTimeOffset ConvertExcelDateTimeOffset(object sourceValue, ColumnBase destinationColumn)
    {
        if (sourceValue is DateTime dateTime && dateTime.Kind != DateTimeKind.Unspecified)
        {
            return new DateTimeOffset(dateTime);
        }

        if (sourceValue is string text)
        {
            text = text.Trim();
            if (DateTimeOffset.TryParseExact(text, ExcelOffsetDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }

            if (DateTimeOffset.TryParseExact(text, ExcelUtcDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            {
                return parsed;
            }
        }

        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected a year-first date-time with Z or an explicit offset.");
    }

    private static decimal ConvertExcelDecimal(object sourceValue, ColumnBase destinationColumn)
    {
        if (TryConvertExcelDecimal(sourceValue, out var result)) return result;
        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected an invariant numeric value.");
    }

    private static long ConvertExcelInt64(object sourceValue, ColumnBase destinationColumn)
    {
        var value = ConvertExcelWholeNumber(sourceValue, destinationColumn, "Int64", long.MinValue, long.MaxValue);
        return decimal.ToInt64(value);
    }

    private static int ConvertExcelInt32(object sourceValue, ColumnBase destinationColumn)
    {
        var value = ConvertExcelWholeNumber(sourceValue, destinationColumn, "Int32", int.MinValue, int.MaxValue);
        return decimal.ToInt32(value);
    }

    private static short ConvertExcelInt16(object sourceValue, ColumnBase destinationColumn)
    {
        var value = ConvertExcelWholeNumber(sourceValue, destinationColumn, "Int16", short.MinValue, short.MaxValue);
        return decimal.ToInt16(value);
    }

    private static byte ConvertExcelByte(object sourceValue, ColumnBase destinationColumn)
    {
        var value = ConvertExcelWholeNumber(sourceValue, destinationColumn, "Byte", byte.MinValue, byte.MaxValue);
        return decimal.ToByte(value);
    }

    private static decimal ConvertExcelWholeNumber(object sourceValue, ColumnBase destinationColumn, string typeName, decimal minimum, decimal maximum)
    {
        if (TryConvertExcelDecimal(sourceValue, out var value)
            && value == decimal.Truncate(value)
            && value >= minimum
            && value <= maximum)
        {
            return value;
        }

        throw InvalidExcelConversion(sourceValue, destinationColumn, $"The value must be an exact {typeName} value within range.");
    }

    private static bool ConvertExcelBoolean(object sourceValue, ColumnBase destinationColumn)
    {
        if (sourceValue is string text)
        {
            text = text.Trim();
            if (text.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) || text == "1") return true;
            if (text.Equals(bool.FalseString, StringComparison.OrdinalIgnoreCase) || text == "0") return false;
        }
        else
        {
            try
            {
                var numericValue = Convert.ToDecimal(sourceValue, CultureInfo.InvariantCulture);
                if (numericValue == decimal.One) return true;
                if (numericValue == decimal.Zero) return false;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
            }
        }

        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected true, false, 1, or 0.");
    }

    private static double ConvertExcelDouble(object sourceValue, ColumnBase destinationColumn)
    {
        if (TryConvertExcelDouble(sourceValue, out var result)) return result;
        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected a finite invariant numeric value.");
    }

    private static float ConvertExcelSingle(object sourceValue, ColumnBase destinationColumn)
    {
        if (TryConvertExcelDouble(sourceValue, out var value))
        {
            var result = (float)value;
            if (float.IsFinite(result)) return result;
        }

        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected a finite invariant Single value within range.");
    }

    private static Guid ConvertExcelGuid(object sourceValue, ColumnBase destinationColumn)
    {
        if (sourceValue is string text && Guid.TryParse(text.Trim(), out var result)) return result;
        throw InvalidExcelConversion(sourceValue, destinationColumn, "Expected a valid GUID.");
    }

    private static bool TryConvertExcelDecimal(object sourceValue, out decimal result)
    {
        if (sourceValue is string text)
        {
            return decimal.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);
        }

        if (IsNumericExcelValue(sourceValue))
        {
            try
            {
                result = Convert.ToDecimal(sourceValue, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException)
            {
            }
        }

        result = default;
        return false;
    }

    private static bool TryConvertExcelDouble(object sourceValue, out double result)
    {
        bool converted;
        if (sourceValue is string text)
        {
            converted = double.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);
        }
        else if (IsNumericExcelValue(sourceValue))
        {
            try
            {
                result = Convert.ToDouble(sourceValue, CultureInfo.InvariantCulture);
                converted = true;
            }
            catch (Exception ex) when (ex is InvalidCastException or OverflowException)
            {
                result = default;
                converted = false;
            }
        }
        else
        {
            result = default;
            converted = false;
        }

        return converted && double.IsFinite(result);
    }

    private static bool IsNumericExcelValue(object value)
    {
        return value is byte or sbyte or short or ushort or int or uint or long or ulong or decimal or double or float;
    }

    private static FormatException InvalidExcelConversion(object sourceValue, ColumnBase destinationColumn, string? requirement = null)
    {
        var displayValue = sourceValue is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : sourceValue.ToString();
        var message = $"Excel value '{displayValue}' ({sourceValue.GetType().Name}) cannot be converted to {destinationColumn.SQLMetadata.SQLType} for column '{destinationColumn.Name}'.";
        if (!string.IsNullOrWhiteSpace(requirement)) message += $" {requirement}";
        return new FormatException(message);
    }

}
