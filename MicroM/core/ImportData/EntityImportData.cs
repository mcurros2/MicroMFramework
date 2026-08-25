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
        if (destinationColumn.SQLMetadata.SQLType.IsTypeAccepted(sourceValue.GetType())) return sourceValue;

        return destinationColumn.SQLMetadata.SQLType switch
        {
            SqlDbType.DateTime or SqlDbType.DateTime2 or SqlDbType.Date or SqlDbType.SmallDateTime => ConvertExcelDateTime(sourceValue, destinationColumn.Name),
            SqlDbType.Decimal or SqlDbType.Money or SqlDbType.SmallMoney => ConvertExcelDecimal(sourceValue, destinationColumn.Name),
            SqlDbType.Int => ConvertExcelInt32(sourceValue, destinationColumn.Name),
            SqlDbType.Bit => ConvertExcelBoolean(sourceValue, destinationColumn.Name),
            SqlDbType.Float => ConvertExcelDouble(sourceValue, destinationColumn.Name),
            _ => throw InvalidExcelConversion(sourceValue, destinationColumn)
        };
    }

    private static DateTime ConvertExcelDateTime(object sourceValue, string destinationColumnName)
    {
        if (sourceValue is string text
            && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        try
        {
            var serialValue = Convert.ToDouble(sourceValue, CultureInfo.InvariantCulture);
            if (double.IsFinite(serialValue)) return DateTime.FromOADate(serialValue);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or ArgumentException or OverflowException)
        {
        }

        throw new FormatException($"Excel value '{sourceValue}' cannot be converted to DateTime for column '{destinationColumnName}'.");
    }

    private static decimal ConvertExcelDecimal(object sourceValue, string destinationColumnName)
    {
        if (sourceValue is string text
            && decimal.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        try
        {
            return Convert.ToDecimal(sourceValue, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            throw new FormatException($"Excel value '{sourceValue}' cannot be converted to Decimal for column '{destinationColumnName}'.", ex);
        }
    }

    private static int ConvertExcelInt32(object sourceValue, string destinationColumnName)
    {
        if (sourceValue is string text
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        try
        {
            var numericValue = Convert.ToDecimal(sourceValue, CultureInfo.InvariantCulture);
            if (numericValue != decimal.Truncate(numericValue) || numericValue < int.MinValue || numericValue > int.MaxValue)
            {
                throw new OverflowException();
            }

            return decimal.ToInt32(numericValue);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            throw new FormatException($"Excel value '{sourceValue}' must be an exact Int32 value for column '{destinationColumnName}'.", ex);
        }
    }

    private static bool ConvertExcelBoolean(object sourceValue, string destinationColumnName)
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

        throw new FormatException($"Excel value '{sourceValue}' must be true, false, 1, or 0 for column '{destinationColumnName}'.");
    }

    private static double ConvertExcelDouble(object sourceValue, string destinationColumnName)
    {
        double result;
        if (sourceValue is string text)
        {
            if (!double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            {
                throw new FormatException($"Excel value '{sourceValue}' cannot be converted to Double for column '{destinationColumnName}'.");
            }
        }
        else
        {
            try
            {
                result = Convert.ToDouble(sourceValue, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                throw new FormatException($"Excel value '{sourceValue}' cannot be converted to Double for column '{destinationColumnName}'.", ex);
            }
        }

        if (!double.IsFinite(result))
        {
            throw new FormatException($"Excel value '{sourceValue}' must be finite for column '{destinationColumnName}'.");
        }

        return result;
    }

    private static FormatException InvalidExcelConversion(object sourceValue, ColumnBase destinationColumn)
    {
        return new FormatException($"Excel value '{sourceValue}' cannot be converted to {destinationColumn.SQLMetadata.SQLType} for column '{destinationColumn.Name}'.");
    }


}
