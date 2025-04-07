using MicroM.Configuration;
using System.Threading.Channels;

namespace MicroM.Data
{
    /// <summary>
    /// DataResultChannel es la forma de consumir el resultado de una consulta en paralelo con la transmisión de ese resultado al cliente.
    /// Contiene un header con los nombres de las columnas y un <![CDATA[Channel<object[]>]]> que contiene los valores de las columnas para
    /// cada registro. El objetivo es leer los resultados de la base de datos sin detenerse y escribirlos en el canal. La aplicación
    /// cliente lee de este canal de manera asincrónica. El buffer por defecto es de 500000 registros.
    /// </summary>
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
