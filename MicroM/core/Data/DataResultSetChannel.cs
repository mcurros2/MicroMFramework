using MicroM.Configuration;
using System.Threading.Channels;

namespace MicroM.Data
{
    /// <summary>
    /// Manages a channel that streams multiple <see cref="DataResultChannel"/> instances.
    /// </summary>
    public class DataResultSetChannel
    {
        /// <summary>Gets the channel containing result sets.</summary>
        public Channel<DataResultChannel> Results { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataResultSetChannel"/> class.
        /// </summary>
        /// <param name="buffer_results">Optional buffer size for result sets.</param>
        public DataResultSetChannel(int? buffer_results = null)
        {
            buffer_results ??= DataDefaults.DefaultChannelResultsBuffer;
            var options = new BoundedChannelOptions(buffer_results.Value)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            };

            Results = Channel.CreateBounded<DataResultChannel>(options);

        }
    }
}
