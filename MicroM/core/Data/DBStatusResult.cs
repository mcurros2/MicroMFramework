namespace MicroM.Data
{

    public class DBStatusResult
    {
        public bool Failed { get; init; } = false;
        public bool AutonumReturned { get; init; } = false;

        public List<DBStatus>? Results { get; init; }

        public DBStatusResult() { }
    }
}
