using MicroM.Core;
using MicroM.Data;
using System.Text.Json;

namespace MicroM.AutomatedTests;

public sealed class EntityJsonTestData
{
    public string[] headers { get; set; } = [];

    public JsonElement[][] records { get; set; } = [];
}

public static class EntityJsonTestDataExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string GetTestDataFilePath(this EntityBase entity, string test_data_folder)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(test_data_folder);
        string entityName = entity.Def.Name;
        string filename = $"{entityName}Data.json";
        return Path.Combine(test_data_folder, filename);
    }

    public static async Task<EntityJsonTestData> LoadJsonEntityTestData(this EntityBase entity, string test_data_file)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(test_data_file);

        if (!File.Exists(test_data_file))
        {
            throw new FileNotFoundException($"Test data file not found for entity '{entity.Def.Name}'. Expected: {test_data_file}", test_data_file);
        }

        using var json_stream = File.OpenRead(test_data_file);

        var data = await JsonSerializer.DeserializeAsync<EntityJsonTestData>(json_stream, _jsonOptions) ?? throw new InvalidOperationException($"Invalid or empty JSON in '{test_data_file}'.");

        ValidateHeaders(data, entity.Def.Columns, entity.Def.Name, test_data_file);
        return data;
    }

    public static void ApplyRecordToColumns(this EntityJsonTestData data, JsonElement[] record, IReadonlyOrderedDictionary<ColumnBase> columns)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(columns);

        int max = Math.Min(data.headers.Length, record.Length);
        for (int i = 0; i < max; i++)
        {
            string header = data.headers[i];

            if (!columns.TryGetValue(header, out var column))
            {
                // Should not happen after validation, but keep resilient.
                throw new InvalidOperationException($"Unexpected header '{header}' not found in columns.");
            }

            var value = record[i];
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            {
                column!.ValueObject = null;
            }
            else
            {
                column!.ValueObject = value;
            }
        }
    }

    public static string ToRecordValuesString(this EntityJsonTestData data, JsonElement[] record, string[]? properties_filter = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(record);

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < data.headers.Length; i++)
        {
            headerIndex[data.headers[i]] = i;
        }

        IEnumerable<string> names = properties_filter is { Length: > 0 } ? properties_filter : data.headers;

        var values = new List<string>();
        foreach (var name in names)
        {
            if (!headerIndex.TryGetValue(name, out int idx) || idx >= record.Length)
            {
                values.Add($"{name}: <missing>");
                continue;
            }

            string strValue = record[idx].ValueKind switch
            {
                JsonValueKind.String => record[idx].GetString() ?? "NULL",
                JsonValueKind.Null => "NULL",
                _ => record[idx].GetRawText()
            };

            values.Add($"{name}: {strValue}");
        }

        return string.Join(" | ", values);
    }

    private static void ValidateHeaders(EntityJsonTestData data, IReadonlyOrderedDictionary<ColumnBase> columns, string entityName, string path)
    {
        var columnsByName = columns.Values.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        var unknownHeaders = data.headers
            .Where(h => !columnsByName.ContainsKey(h))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (unknownHeaders.Length > 0)
        {
            throw new Exception(
                $"Invalid test data headers for entity '{entityName}' in file '{path}'. Unknown header(s): {string.Join(", ", unknownHeaders)}");
        }
    }
}