using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Excel;
using MicroM.Extensions;
using MicroM.Web.Services;
using System.Data;
using System.Globalization;
using System.Text;

namespace MicroM.ImportData
{
    /// <summary>
    /// Provides extension methods for importing data into entities from CSV or Excel sources.
    /// </summary>
    public static class EntityImportData
    {
        /// <summary>
        /// Maps CSV data into the entity's columns.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">Target entity.</param>
        /// <param name="data">Key-value pairs representing a CSV row.</param>
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

        /// <summary>
        /// Imports a list of CSV rows into the specified entity.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">Target entity instance.</param>
        /// <param name="data">Parsed CSV rows.</param>
        /// <param name="options">Application options.</param>
        /// <param name="claims">Claims to pass to the API.</param>
        /// <param name="api">Web API services.</param>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="parentKeys">Optional parent key values.</param>
        /// <param name="ct">Token to monitor for cancellation requests.</param>
        /// <returns>Result of the import operation.</returns>
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

        /// <summary>
        /// Imports data from an Excel stream into the specified entity.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entity">Target entity instance.</param>
        /// <param name="excelStream">Stream containing the Excel data.</param>
        /// <param name="sheetName">Optional sheet name to read.</param>
        /// <param name="initialRow">Starting row index (1-based).</param>
        /// <param name="options">Application options.</param>
        /// <param name="claims">Claims to pass to the API.</param>
        /// <param name="api">Web API services.</param>
        /// <param name="app_id">Application identifier.</param>
        /// <param name="parentKeys">Optional parent key values.</param>
        /// <param name="ct">Token to monitor for cancellation requests.</param>
        /// <returns>Result of the import operation.</returns>
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
            CSVImportResult result = new();

            var client = entity.Client;

            try
            {
                var rows = ExcelReader.ReadExcelAsync(excelStream, sheetName, initialRow ?? 1);

                object?[]? headerRow = null;
                int rowIndex = 0;

                await client.Connect(ct);

                await foreach (var row in rows)
                {
                    if (row == null || row.Length == 0)
                    {
                        result.ProcessedCount++;
                        continue;
                    }

                    if (headerRow == null)
                    {
                        headerRow = row;
                        result.ProcessedCount++;
                        continue;
                    }

                    var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < headerRow.Length && i < row.Length; i++)
                    {
                        var key = headerRow[i]?.ToString() ?? $"Column{i}";
                        var value = row[i];
                        data[key] = value!;
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
    }
}

