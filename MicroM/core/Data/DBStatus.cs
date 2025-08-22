
using System.Text.Json.Serialization;

namespace MicroM.Data
{

    public enum DBStatusCodes
    {
        OK = 0,
        RecordHasChanged = 4,
        Error = 11,
        Autonum = 15
    }

    public class DBStatus
    {
        public DBStatusCodes Status { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; init; } = null;

        public DBStatus(DBStatusCodes status, string? message = null)
        {
            Status = status;
            Message = message;
        }

        public DBStatus() { }
    }

}
