using MicroM.Data;

namespace MicroM.Web.Authentication;

public enum TotpServiceResultStatus
{
    Success,
    AppNotFound,
    UnsupportedAuthenticator,
    InvalidUser,
    SetupNotStarted,
    InvalidCode,
    DatabaseFailure
}

public sealed class TotpServiceResult
{
    public TotpServiceResultStatus Status { get; init; }

    public TotpSetupStartResponse? SetupResponse { get; init; }

    public DBStatusResult? DatabaseResult { get; init; }

    public static TotpServiceResult Success(TotpSetupStartResponse? setupResponse = null)
    {
        return new()
        {
            Status = TotpServiceResultStatus.Success,
            SetupResponse = setupResponse
        };
    }

    public static TotpServiceResult Failed(TotpServiceResultStatus status, DBStatusResult? databaseResult = null)
    {
        return new()
        {
            Status = status,
            DatabaseResult = databaseResult
        };
    }
}
