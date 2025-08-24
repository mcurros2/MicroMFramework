using MicroM.Configuration;
using System.Threading.Channels;

namespace MicroM.Data
{
    /// <summary>
    /// Provides a channel-based buffer for streaming tabular records.
    /// </summary>
    public class DataResultChannel
    {
        /// <summary>Gets the column headers for the streamed records.</summary>
        public readonly string[] Header;

        /// <summary>Gets the channel where records are written.</summary>
        public Channel<object[]> Records { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataResultChannel"/> class.
        /// </summary>
        /// <param name="columns">Number of columns in the result.</param>
        /// <param name="buffer_records">Optional record buffer size.</param>
        public DataResultChannel(int columns, int? buffer_records = null)
        {
            buffer_records ??= DataDefaults.DefaultChannelRecordsBuffer;
            Header = new string[columns];

            var options = new BoundedChannelOptions(buffer_records.Value)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false
            };

            Records = Channel.CreateBounded<object[]>(options);

        }
    }
}
