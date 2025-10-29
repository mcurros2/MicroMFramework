using System.Data.Common;
using System.Runtime.CompilerServices;
using static MicroM.Data.IEntityClient;

namespace MicroM.Data;

public static class DataMappingProvider
{
    private static string GetResultingHeaderName(string original_name, int index, bool spaceToUnderscore)
    {
        if (string.IsNullOrEmpty(original_name))
        {
            return $"Column{index}";
        }
        else
        {
            return spaceToUnderscore ? original_name.Replace(" ", "_") : original_name;
        }
    }

    public static (string[] headers, string[] typeInfo) GetHeaders(DbDataReader reader, bool spaceToUnderscore = false)
    {
        string[] headers = new string[reader.FieldCount];
        string[] typeInfo = new string[reader.FieldCount];
        for (int x = 0; x < reader.FieldCount; x++)
        {
            string original_name = reader.GetName(x);
            string type = reader.GetDataTypeName(x);
            typeInfo[x] = type;

            string resulting_name = GetResultingHeaderName(original_name, x, spaceToUnderscore);

            if (headers.Contains(resulting_name))
            {
                throw new InvalidOperationException($"Duplicate column name when replacing spaces with underscores. Original: {original_name} Replaced: {resulting_name}");
            }

            headers[x] = resulting_name;
        }

        return (headers, typeInfo);
    }

    public static HashSet<string> GetHeadersHashSet(DbDataReader reader, bool spaceToUnderscore = false)
    {
        HashSet<string> headers = new(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (int x = 0; x < reader.FieldCount; x++)
        {
            string original_name = reader.GetName(x);
            string resulting_name = GetResultingHeaderName(original_name, x, spaceToUnderscore);

            if (headers.Contains(resulting_name))
            {
                throw new InvalidOperationException($"Duplicate column name when replacing spaces with underscores. Original: {original_name} Replaced: {resulting_name}");
            }

            headers.Add(resulting_name);
        }

        return headers;
    }

    public static async IAsyncEnumerable<T> GetResult<T>(ValueReader vr, MapResult<T> mapper, [EnumeratorCancellation] CancellationToken ct)
    {
        if (await vr._reader.ReadAsync(ct))
        {
            var (headers, typeInfo) = GetHeaders(vr._reader);
            do
            {
                ct.ThrowIfCancellationRequested();
                T record = await mapper(vr, headers, typeInfo, ct);
                yield return record;
            }
            while (await vr._reader.ReadAsync(ct));
        }
    }

}
