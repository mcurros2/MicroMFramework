

namespace MicroM.DataDictionary.Entities.MicromUsers
{
    public record RefreshTokenResult
    {
        public LoginAttemptStatus Status { get; set; } = LoginAttemptStatus.Unknown;
        public string? Message { get; set; } = null;
        public string? RefreshToken { get; set; } = null;
        public DateTime? RefreshExpiration { get; set; } = null;
    }


}
