using MicroM.Core;
using MicroM.Data;
using MicroM.Database;
using MicroM.Extensions;
using MicroM.Web.Services;
using System.Diagnostics;
using System.Text;

namespace MicroM.AutomatedTests;

public static class EntityTests
{

    private static string[] GetColNamesArray(this CustomOrderedDictionary<ColumnBase> columns)
    {
        return [.. columns.Values.Select(c => c.Name)];
    }

    public static async Task FullTest<T>(IEntityClient ec, IMicroMEncryption enc, string data_group_folder, string schema_name, string test_data_folder, bool seed_test_data, CancellationToken ct)
        where T : EntityBase, new()
    {
        T entity = new();
        entity.Init(ec, enc, schema_name);

        await FullTest(entity, test_data_folder, seed_test_data, ct);
    }

    public static async Task FullTest(EntityBase entity, string test_data_folder, bool seed_test_data, CancellationToken ct, Action<string>? log = null)
    {
        var test_data_file = entity.GetTestDataFilePath(test_data_folder);
        var testData = await entity.LoadJsonEntityTestData(test_data_file);

        var pk_cols = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
        var pk_cols_names_array = pk_cols.GetColNamesArray();

        const string test_version = "1.1.2";

        log?.Invoke($"*** {entity.Def.Name}: Running tests with test data from file {test_data_file}. version {test_version}");

        ColumnBase? change_column = null;
        foreach (var column in entity.Def.Columns.Values)
        {
            if (!column.ColumnMetadata.HasFlag(ColumnFlags.PK) && column.SystemType == typeof(string) && column.SQLMetadata.Size >= 80)
            {
                change_column = column;
                break;
            }
        }

        log?.Invoke($"{entity.Def.Name}: Change column used {change_column?.Name}.");

        if (entity.Def.AutonumColumn != null)
        {
            log?.Invoke($"{entity.Def.Name}: Autonum column {entity.Def.AutonumColumn.Name} will preserve first record.");

        }

        bool first_record = true;
        foreach (var record in testData.records)
        {
            if (ct.IsCancellationRequested) break;

            testData.ApplyRecordToColumns(record, entity.Def.Columns);

            log?.Invoke($"{entity.Def.Name}: Inserting data for record with PK values {testData.ToRecordValuesString(record, pk_cols_names_array)} at time {DateTime.Now:O}.");

            var time_at_insert = DateTime.Now;

            await entity.InsertData(ct, throw_dbstat_exception: true);

            var get_result = await entity.GetData(ct);
            if (!get_result)
            {
                throw new Exception($"INSERT: Can't read record after insertion. ID {testData.ToRecordValuesString(record, pk_cols_names_array)}");
            }

            // save dt_lu value for later comparison after update
            var dt_lu_value = entity.Def.dt_lu.Value;
            log?.Invoke($"{entity.Def.Name}: Data inserted lu {dt_lu_value:O}.");

            var stop_watch = Stopwatch.StartNew();

            log?.Invoke($"{entity.Def.Name}: Looking up data for record with PK values {testData.ToRecordValuesString(record, pk_cols_names_array)}.");

            var lookup_result = await entity.LookupData(ct);
            bool lookup_success = !lookup_result.IsNullOrEmpty();
            if (!lookup_success)
            {
                throw new Exception($"LOOKUP: Can't lookup record after insertion. ID {testData.ToRecordValuesString(record, pk_cols_names_array)}");
            }

            change_column?.ValueObject = $"changed {change_column.ValueObject}";

            // Compare the elapsed ms. Wait if needed
            stop_watch.Stop();
            var remaining_ms = 5.0 - stop_watch.Elapsed.TotalMilliseconds;
            if (remaining_ms > 0.0)
            {
                var awaited = (int)Math.Ceiling(remaining_ms);
                await Task.Delay(awaited, ct);
                log?.Invoke($"{entity.Def.Name}: Delayed {awaited} ms to ensure dt_lu value will be different after update. Elapsed: {stop_watch.Elapsed.TotalMilliseconds} ms. Insert dt_lu {dt_lu_value:O}, Now: {DateTime.Now:O}");
            }

            log?.Invoke($"{entity.Def.Name}: Updating data for record with PK values {testData.ToRecordValuesString(record, pk_cols_names_array)} at time {DateTime.Now:O}, elapsed ms since insert {(DateTime.Now - time_at_insert).TotalMilliseconds}.");

            await entity.UpdateData(ct, throw_dbstat_exception: true);

            var update_result = await entity.GetData(ct);
            if (!update_result)
            {
                throw new Exception($"UPDATE: Can't read record after update. ID {testData.ToRecordValuesString(record, pk_cols_names_array)}");
            }

            if (change_column != null)
            {
                bool change_column_updated = Equals(change_column.ValueObject, entity.Def.Columns[change_column.Name]!.ValueObject);
                if (!change_column_updated)
                {
                    throw new Exception($"UPDATE: The column {change_column.Name} was not updated after the update. ID in data {testData.ToRecordValuesString(record, pk_cols_names_array)}");
                }
            }

            if (Equals(dt_lu_value.Ticks, entity.Def.dt_lu.Value.Ticks))
            {
                throw new Exception($"UPDATE: The column dt_lu was not updated after the update. ID in data {testData.ToRecordValuesString(record, pk_cols_names_array)}. Time elapsed between insert and update: {(DateTime.Now - time_at_insert).TotalMilliseconds} ms. Insert LU: {dt_lu_value:O}, before update: {entity.Def.dt_lu.Value:O}");
            }

            log?.Invoke($"{entity.Def.Name}: Deleting data for record with PK values {testData.ToRecordValuesString(record, pk_cols_names_array)} at time {DateTime.Now:O}.");

            if (entity.Def.AutonumColumn != null && first_record)
            {
                log?.Invoke($"{entity.Def.Name}: Skipping deletion of first record with PK values {testData.ToRecordValuesString(record, pk_cols_names_array)} because of autonum column {entity.Def.AutonumColumn.Name}.");
                first_record = false;
            }
            else
            {
                await entity.DeleteData(ct, throw_dbstat_exception: true);

                var delete_result = await entity.GetData(ct);
                if (delete_result)
                {
                    throw new Exception($"DELETE: record still exists after deletion. ID in data {testData.ToRecordValuesString(record, pk_cols_names_array)}");
                }
            }

            if (seed_test_data)
            {
                log?.Invoke($"{entity.Def.Name}: Re-inserting data for record with PK values {testData.ToRecordValuesString(record, pk_cols_names_array)} for seeding test data.");
                // re-insert the record for seeding test data
                await entity.InsertData(ct, throw_dbstat_exception: true);
            }
        }
    }

    public static async Task RunTestsOnEntities(
        this List<IEntityType> entities, string server, string database, string user, string password, IMicroMEncryption enc, string schema_name, string test_data_folder,
        bool seed_test_data, CancellationToken ct, Action<string>? log = null)
    {
        using var ec = new DatabaseClient(server, database, user, password);
        await ec.Connect(ct);

        foreach (var entity_type in entities)
        {
            if (ct.IsCancellationRequested) break;
            var entity = Activator.CreateInstance(entity_type.Type) as EntityBase
                ?? throw new Exception($"Can't create instance of entity type {entity_type.Type.FullName}");

            entity.Init(ec, enc, schema_name);

            await FullTest(entity, test_data_folder, seed_test_data, ct, log);
        }
    }

    public static async Task<string?> SeedTestData(EntityBase entity, string test_data_file, CancellationToken ct, bool ignore_errors = true)
    {
        var testData = await entity.LoadJsonEntityTestData(test_data_file);
        int record_count = 0;
        foreach (var record in testData.records)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                testData.ApplyRecordToColumns(record, entity.Def.Columns);
                await entity.UpdateData(ct, throw_dbstat_exception: false);
            }
            catch (Exception ex)
            {
                if (!ignore_errors)
                {
                    throw;
                }
                return $"Error inserting/updating record for entity {entity.Def.Name} from file {test_data_file}: {ex.Message}";
            }
        }
        record_count++;
        return $"Inserted/Updated {record_count} records for entity {entity.Def.Name} from file {test_data_file}";
    }

    public static async Task<string?> SeedTestData(this CustomOrderedDictionary<DatabaseSchemaCreationOptions<EntityBase>> entities, string test_data_folder, CancellationToken ct, bool ignore_errors = true)
    {
        if (entities == null || entities.Count == 0) return null;

        await entities[0]!.EntityInstance.Client.Connect(ct);

        StringBuilder sb = new();

        foreach (var entity_type in entities)
        {
            if (ct.IsCancellationRequested) break;

            var entity = entity_type.EntityInstance;

            var data_file_path = entity.GetTestDataFilePath(test_data_folder);
            if (File.Exists(data_file_path))
            {
                try
                {
                    if (await entity.HasDataInTable(ct))
                    {
                        sb.AppendLine($"Data already exists for entity {entity.Def.Name} - skipping seeding data from file {data_file_path}");
                        continue;
                    }
                    var result = await SeedTestData(entity, data_file_path, ct);
                    if (result != null)
                    {
                        sb.AppendLine(result);
                    }
                }
                catch (Exception ex)
                {
                    if (!ignore_errors)
                    {
                        throw;
                    }
                    sb.AppendLine($"Error seeding data for entity {entity.Def.Name} from file {data_file_path}: {ex.Message}");
                }
            }
            else
            {
                sb.AppendLine($"Test data file {data_file_path} not found for entity {entity.Def.Name} - not seeding data");
            }
        }

        return sb.ToString();
    }

}
