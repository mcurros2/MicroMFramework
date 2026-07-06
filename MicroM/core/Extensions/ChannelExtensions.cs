using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MicroM.Extensions;

public static class ChannelExtensions
{
    public static async IAsyncEnumerable<object?[]> StreamRows(this ChannelReader<object?[]> rows, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var row in rows.ReadAllAsync(ct))
        {
            yield return row;
        }
    }
}
