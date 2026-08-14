namespace MicroM.Data
{

    public class DBStatusResult
    {
        public bool Failed { get; init; } = false;
        public bool AutonumReturned { get; init; } = false;

        public List<DBStatus>? Results { get; init; }

        public DBStatusResult() { }

        public static DBStatusResult FailedStatus(string message) => new()
        {
            Failed = true,
            Results = [new(DBStatusCodes.Error, message)]
        };

        public static DBStatusResult FailedStatus(List<DBStatus> errors) => new()
        {
            Failed = true,
            Results = errors
        };

        public static DBStatusResult SuccessStatus(string message = "OK") => new()
        {
            Results = [new(DBStatusCodes.OK, message)]
        };

        public static DBStatusResult AutonumStatus(string new_number) => new()
        {
            AutonumReturned = true,
            Results = [new(DBStatusCodes.Autonum, new_number)]
        };

    }
}
