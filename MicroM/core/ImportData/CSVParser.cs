using MicroM.Extensions;

namespace MicroM.ImportData
{

    public static class CSVParser
    {
        public static List<Dictionary<string, string>> Parse(string csvData, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(csvData)) return [];

            string[] lines = csvData.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
            {
                return [];
            }

            string[] headers = lines[0].Split(',');
            for (int x = 0; x < headers.Length; x++)
            {
                headers[x] = headers[x].Trim().Unquote(true);
            }

            List<Dictionary<string, string>> result = [];
            for (int i = 1; i < lines.Length; i++)
            {
                ct.ThrowIfCancellationRequested();

                string[] values = lines[i].Split(',');
                if (values.Length != headers.Length)
                {
                    return [];
                }

                Dictionary<string, string> row = [];
                for (int j = 0; j < headers.Length; j++)
                {
                    row.Add(headers[j], values[j].Unquote(true));
                }

                result.Add(row);
            }

            return result;
        }

        public static async Task<List<Dictionary<string, string>>> ParseFile(string file_path, CancellationToken ct)
        {
            var data = await File.ReadAllTextAsync(file_path, System.Text.Encoding.UTF8, ct);
            return Parse(data, ct);
        }
    }
}
