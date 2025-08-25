namespace MicroM.DataDictionary.Entities.MicromUsers
{
    /// <summary>
    /// Status codes representing the outcome of a login attempt.
    /// </summary>
    public enum LoginAttemptStatus
    {
        /// <summary>
        /// Login state was updated successfully.
        /// </summary>
        Updated = 0,

        /// <summary>
        /// Provided refresh token is invalid.
        /// </summary>
        InvalidRefreshToken = 8,

        /// <summary>
        /// Refresh token has expired.
        /// </summary>
        RefreshTokenExpired = 9,

        /// <summary>
        /// Maximum number of refresh attempts reached.
        /// </summary>
        MaxRefreshReached = 10,

        /// <summary>
        /// User identifier was not found.
        /// </summary>
        UserIDNotFound = 11,

        /// <summary>
        /// Account is locked.
        /// </summary>
        AccountLocked = 13,

        /// <summary>
        /// Account is disabled.
        /// </summary>
        AccountDisabled = 14,

        /// <summary>
        /// Refresh token is valid.
        /// </summary>
        RefreshTokenValid = 15,

        /// <summary>
        /// Login succeeded.
        /// </summary>
        LoggedInOK = 16,

        /// <summary>
        /// Unknown result.
        /// </summary>
        Unknown = -1
    }

}
