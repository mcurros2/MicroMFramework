using MicroM.Configuration;
using System.Threading.Channels;

namespace MicroM.Data;

public class DataResultChannel
{
    public string[] Header { get; private set; }
    public string[] typeInfo { get; private set; }

    public Channel<object?[]> records { get; private set; }

    public DataResultChannel(int columns, int? records_capacity = null, string[]? headers = null, string[]? type_info = null)
    {
        records_capacity ??= DataDefaults.DefaultChannelRecordsBuffer;
        Header = headers ?? new string[columns];
        typeInfo = type_info ?? new string[columns];

        if (Header.Length != typeInfo.Length)
        {
            throw new ArgumentException("The headers and type_info arrays must have the same length");
        }

        var options = new BoundedChannelOptions(records_capacity.Value)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        };

        records = Channel.CreateBounded<object?[]>(options);
    }
}
