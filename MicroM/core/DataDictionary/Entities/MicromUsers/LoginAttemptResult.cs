
namespace MicroM.DataDictionary.Entities.MicromUsers
{
    /// <summary>
    /// Result returned after attempting to log in.
    /// </summary>
    public record LoginAttemptResult
    {
        /// <summary>
        /// Status of the login attempt.
        /// </summary>
        public LoginAttemptStatus Status { get; set; } = LoginAttemptStatus.Unknown;

        /// <summary>
        /// Optional message describing the result.
        /// </summary>
        public string? Message { get; set; } = null;

        /// <summary>
        /// Refresh token issued when the attempt succeeds.
        /// </summary>
        public string? RefreshToken { get; set; } = null;
    }


}
