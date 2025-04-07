namespace MicroM.DataDictionary.Entities.MicromUsers
{
    public enum LoginAttemptStatus
    {
        Updated = 0,
        InvalidRefreshToken = 8,
        RefreshTokenExpired = 9,
        MaxRefreshReached = 10,
        UserIDNotFound = 11,
        AccountLocked = 13,
        AccountDisabled = 14,
        RefreshTokenValid = 15,
        LoggedInOK = 16,
        Unknown = -1
    }

}
