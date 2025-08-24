using MicroM.Configuration;
using System.Threading.Channels;

namespace MicroM.Data
{
    public class DataResultChannel
    {
        public readonly string[] Header;
        public Channel<object[]> Records { get; private set; }

        public DataResultChannel(int columns, int? buffer_records = null)
        {
            buffer_records ??= DataDefaults.DefaultChannelRecordsBuffer;
            Header = new string[columns];

            var options = new BoundedChannelOptions(buffer_records.Value);
            options.FullMode = BoundedChannelFullMode.Wait;
            options.SingleReader = true;
            options.SingleWriter = true;
            options.AllowSynchronousContinuations = false;

            Records = Channel.CreateBounded<object[]>(options);

        }
    }
}
