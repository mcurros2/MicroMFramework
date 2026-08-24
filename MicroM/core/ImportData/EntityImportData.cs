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
                if (col.Name.IsIn(SystemColumnNames.AsStringArray)) continue;

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

                Dictionary<string, object> data = new(StringComparer.OrdinalIgnoreCase);
                if (useExplicitMapping)
                {
                    foreach (var mapping in resolvedMapping)
                    {
                        data[mapping.DestinationColumnName] = row[mapping.SourceIndex]!;
                    }
                }
                else
                {
                    for (int i = 0; i < headerRow.Length && i < row.Length; i++)
                    {
                        var key = headerRow[i]?.ToString() ?? $"Column{i}";
                        data[key] = row[i]!;
                    }
                }

                try
                {
                    entity.SetColumnValues(data);

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

    internal static IReadOnlyList<ResolvedImportDataMapping> ResolveExcelMapping<T>(
        T entity,
        object?[] headerRow,
        ExcelImportMapping? importMapping) where T : EntityBase
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

            List<int> matchingHeaderIndexes = [];
            for (int i = 0; i < headerRow.Length; i++)
            {
                if (string.Equals(headerRow[i]?.ToString(), mapping.SourceHeader, StringComparison.OrdinalIgnoreCase))
                {
                    matchingHeaderIndexes.Add(i);
                }
            }

            int sourceIndex;
            if (matchingHeaderIndexes.Count == 1)
            {
                sourceIndex = matchingHeaderIndexes[0];
            }
            else if (matchingHeaderIndexes.Count > 1)
            {
                if (!matchingHeaderIndexes.Contains(mapping.SourceIndex))
                {
                    throw new InvalidDataException($"The source header '{mapping.SourceHeader}' is duplicated and source index {mapping.SourceIndex} does not identify one of its columns.");
                }

                sourceIndex = mapping.SourceIndex;
            }
            else
            {
                if (mapping.SourceIndex < 0 || mapping.SourceIndex >= headerRow.Length)
                {
                    throw new InvalidDataException($"The source header '{mapping.SourceHeader}' was not found and source index {mapping.SourceIndex} is outside the worksheet header.");
                }

                sourceIndex = mapping.SourceIndex;
            }

            result.Add(new(sourceIndex, destinationColumn.Name));
        }

        return result;
    }

    internal sealed record ResolvedImportDataMapping(int SourceIndex, string DestinationColumnName);

}
