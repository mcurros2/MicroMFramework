using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Web.Services;

namespace MicroM.AutomatedTests;

public static class EntityTests
{
    private static string GetColNames(this CustomOrderedDictionary<ColumnBase> columns)
    {
        return string.Join(", ", columns.Values.Select(c => c.Name));
    }

    private static string GetColValues(this CustomOrderedDictionary<ColumnBase> columns)
    {
        return string.Join(", ", columns.Values.Select(c => $"{c.Name} = {c.ValueObject}"));
    }

    private static string[] GetColNamesArray(this CustomOrderedDictionary<ColumnBase> columns)
    {
        return [.. columns.Values.Select(c => c.Name)];
    }

    public async static Task FullTest<T, D>(IEntityClient ec, IMicroMEncryption enc, string schema_name, CancellationToken ct) where T : EntityBase, new() where D : BaseSeedData, new()
    {
        T entity = new();
        entity.Init(ec, enc, schema_name);

        var seedData = new D();

        await FullTest(entity, seedData, ct);
    }

    public static async Task FullTest(EntityBase entity, BaseSeedData seedData, CancellationToken ct)
    {
        var records = seedData.GetSeedData();

        var pk_cols = entity.Def.Columns.GetWithFlags(ColumnFlags.PK);
        var pk_cols_names_array = pk_cols.GetColNamesArray();
        string pk_cols_names = pk_cols.GetColNames();

        ColumnBase? change_column = null;
        foreach (var column in entity.Def.Columns.Values)
        {
            if (!column.ColumnMetadata.HasFlag(ColumnFlags.PK) && column.SystemType == typeof(string) && column.SQLMetadata.Size > 80)
            {
                change_column = column;
                break;
            }
        }

        foreach (var record in records)
        {
            record.SetPropertiesValuesTo(entity.Def.Columns);

            await entity.InsertData(ct, throw_dbstat_exception: true);

            var get_result = await entity.GetData(ct);
            if (!get_result)
            {
                throw new Exception($"INSERT: Can't read record after insertion. ID {record.ToPropertiesValuesString(pk_cols_names_array)}");
            }

            var lookup_result = await entity.LookupData(ct);
            bool lookup_success = !lookup_result.IsNullOrEmpty();
            if (!lookup_success)
            {
                throw new Exception($"LOOKUP: Can't lookup record after insertion. ID {record.ToPropertiesValuesString(pk_cols_names_array)}");
            }

            var dt_lu_value = entity.Def.dt_lu.Value;

            change_column?.ValueObject = $"changed {change_column.ValueObject}";

            await entity.UpdateData(ct, throw_dbstat_exception: true);
            var update_result = await entity.GetData(ct);
            if (!update_result)
            {
                throw new Exception($"UPDATE: Can't read record after update. ID {record.ToPropertiesValuesString(pk_cols_names_array)}");
            }

            if (change_column != null)
            {
                bool change_column_updated = Equals(change_column.ValueObject, entity.Def.Columns[change_column.Name]!.ValueObject);
                if (!change_column_updated)
                {
                    throw new Exception($"UPDATE: The column {change_column.Name} was not updated after the update. ID in data {record.ToPropertiesValuesString(pk_cols_names_array)}");
                }
            }

            if (Equals(dt_lu_value.Ticks, entity.Def.dt_lu.Value.Ticks))
            {
                throw new Exception($"UPDATE: The column dt_lu was not updated after the update. ID in data {record.ToPropertiesValuesString(pk_cols_names_array)}");
            }

            await entity.DeleteData(ct, throw_dbstat_exception: true);

            var delete_result = await entity.GetData(ct);
            if (delete_result)
            {
                throw new Exception($"DELETE: record still exists after deletion. ID in data {record.ToPropertiesValuesString(pk_cols_names_array)}");
            }


        }
    }

    public async static Task RunTestsOnEntities(this List<IEntityTestTypePair> entities, string server, string database, string user, string password, IMicroMEncryption enc, string schema_name, CancellationToken ct)
    {
        using var ec = new DatabaseClient(server, database, user, password);

        foreach (var pair in entities)
        {
            var entity = Activator.CreateInstance(pair.EntityType) as EntityBase ?? throw new Exception($"Can't create instance of entity type {pair.EntityType.FullName}");
            entity.Init(ec, enc, schema_name);

            var seedData = Activator.CreateInstance(pair.DataType) as BaseSeedData ?? throw new Exception($"Can't create instance of seed data type {pair.DataType.FullName}");

            await FullTest(entity, seedData, ct);
        }
    }
}
