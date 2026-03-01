using MicroM.Configuration;
using System.Threading.Channels;

namespace MicroM.Data;

public class DataResultSetChannel
{
    public Channel<DataResultChannel> Results { get; private set; }

    public DataResultSetChannel(int? capacity = null)
    {
        capacity ??= DataDefaults.DefaultChannelRecordsBuffer;
        var options = new BoundedChannelOptions(capacity.Value)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        };

        Results = Channel.CreateBounded<DataResultChannel>(options);

    }

}
