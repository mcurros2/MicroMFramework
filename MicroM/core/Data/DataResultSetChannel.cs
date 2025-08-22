using MicroM.Configuration;
using System.Threading.Channels;

namespace MicroM.Data
{
    public class DataResultSetChannel
    {
        public Channel<DataResultChannel> Results { get; private set; }

        public DataResultSetChannel(int? buffer_results = null)
        {
            buffer_results ??= DataDefaults.DefaultChannelResultsBuffer;
            var options = new BoundedChannelOptions(buffer_results.Value);
            options.FullMode = BoundedChannelFullMode.Wait;
            options.SingleReader = true;
            options.SingleWriter = true;
            options.AllowSynchronousContinuations = false;

            Results = Channel.CreateBounded<DataResultChannel>(options);

        }
    }
}
