using MicroM.Extensions;

namespace MicroM.ImportData
{
    /// <summary>
    /// Provides helpers for parsing CSV content into dictionaries.
    /// </summary>
    public static class CSVParser
    {
        /// <summary>
        /// Parses CSV data into a list of dictionaries.
        /// </summary>
        /// <param name="csvData">The raw CSV data.</param>
        /// <param name="ct">Token to monitor for cancellation requests.</param>
        /// <returns>A list of dictionaries representing rows.</returns>
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

        /// <summary>
        /// Reads a CSV file and parses its content.
        /// </summary>
        /// <param name="file_path">The path to the CSV file.</param>
        /// <param name="ct">Token to monitor for cancellation requests.</param>
        /// <returns>A list of dictionaries representing rows.</returns>
        public static async Task<List<Dictionary<string, string>>> ParseFile(string file_path, CancellationToken ct)
        {
            var data = await File.ReadAllTextAsync(file_path, System.Text.Encoding.UTF8, ct);
            return Parse(data, ct);
        }
    }
}
