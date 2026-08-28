using System.Text;

namespace MicroM.ImportData;

public sealed record CSVTable(string[] Headers, List<string[]> Rows);

public static class CSVParser
{
    public static CSVTable ParseTable(string csvData, int initialRow, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialRow, 1);

        List<string[]> rows = [.. ParseRows(csvData, ct).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))];

        if (rows.Count < initialRow) return new([], []);

        string[] headers = [.. rows[initialRow - 1].Select(header => header.Trim())];

        if (headers.Length > 0) headers[0] = headers[0].TrimStart('\uFEFF');

        List<string[]> dataRows = [];
        foreach (string[] row in rows.Skip(initialRow))
        {
            ct.ThrowIfCancellationRequested();
            if (row.Length != headers.Length)
            {
                throw new InvalidDataException($"CSV row {dataRows.Count + initialRow + 1} contains {row.Length} columns; expected {headers.Length}.");
            }
            dataRows.Add(row);
        }

        return new(headers, dataRows);
    }

    public static List<Dictionary<string, string>> Parse(string csvData, CancellationToken ct)
    {
        CSVTable table = ParseTable(csvData, 1, ct);
        List<Dictionary<string, string>> result = [];

        foreach (string[] values in table.Rows)
        {
            Dictionary<string, string> row = [];
            for (int index = 0; index < table.Headers.Length; index++)
            {
                row.Add(table.Headers[index], values[index]);
            }
            result.Add(row);
        }

        return result;
    }

    public static async Task<CSVTable> ParseFileTable(Stream fileStream, int initialRow, CancellationToken ct)
    {
        using StreamReader reader = new(fileStream, Encoding.UTF8);
        string data = await reader.ReadToEndAsync(ct);
        return ParseTable(data, initialRow, ct);
    }

    public static async Task<List<Dictionary<string, string>>> ParseFile(Stream fileStream, CancellationToken ct)
    {
        using StreamReader reader = new(fileStream, Encoding.UTF8);
        string data = await reader.ReadToEndAsync(ct);
        return Parse(data, ct);
    }

    private static List<string[]> ParseRows(string csvData, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(csvData)) return [];

        List<string[]> rows = [];
        List<string> row = [];
        StringBuilder field = new();
        bool inQuotes = false;

        void AddField()
        {
            row.Add(field.ToString());
            field.Clear();
        }

        void AddRow()
        {
            AddField();
            rows.Add([.. row]);
            row.Clear();
        }

        for (int index = 0; index < csvData.Length; index++)
        {
            if ((index & 0x3FFF) == 0) ct.ThrowIfCancellationRequested();
            char character = csvData[index];

            if (inQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < csvData.Length && csvData[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(character);
                }
                continue;
            }

            if (character == '"' && field.Length == 0)
            {
                inQuotes = true;
            }
            else if (character == ',')
            {
                AddField();
            }
            else if (character == '\n')
            {
                AddRow();
            }
            else if (character != '\r')
            {
                field.Append(character);
            }
        }

        if (inQuotes) throw new InvalidDataException("The CSV file contains an unterminated quoted value.");
        if (field.Length > 0 || row.Count > 0) AddRow();
        return rows;
    }
}
