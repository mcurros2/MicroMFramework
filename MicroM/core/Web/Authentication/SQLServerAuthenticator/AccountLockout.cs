using MicroM.DataDictionary.Entities.MicromUsers;

namespace MicroM.Web.Authentication
{
    public class AccountLockout
    {
        int _badlogon_attempts = 0;
        DateTime? _locked_until = null;

        string? _RefreshToken = null;
        DateTime? _RefreshTokenExpiration = null;
        int _RefreshTokenValidationCount = 0;

        public bool isAccountLocked()
        {
            if (_locked_until == null) return false;
            if (_locked_until < DateTime.Now) return true;
            return false;
        }

        public void unlockAccount()
        {
            _locked_until = null;
            _badlogon_attempts = 0;
        }

        public void incrementBadLogonAndLock()
        {
            _badlogon_attempts++;
            if (_badlogon_attempts > 10)
            {
                _locked_until = DateTime.Now.AddMinutes(15);
            }
        }

        public LoginAttemptStatus validateRefreshToken(string refreshToken, int max_refresh_count)
        {
            var result = LoginAttemptStatus.Unknown;

            if (refreshToken == null || _RefreshToken == null || _RefreshToken != refreshToken)
            {
                result = LoginAttemptStatus.InvalidRefreshToken;
            }
            else if (_RefreshTokenExpiration > DateTime.Now)
            {
                result = LoginAttemptStatus.RefreshTokenExpired;
            }
            else if (_RefreshTokenValidationCount > max_refresh_count)
            {
                result = LoginAttemptStatus.MaxRefreshReached;
            }
            else if (_RefreshToken == refreshToken)
            {
                result = LoginAttemptStatus.RefreshTokenValid;
            }

            return result;
        }

        public void incrementRefreshTokenValidationCount()
        {
            _RefreshTokenValidationCount++;
        }

        public void clearRefreshToken()
        {
            _RefreshToken = null;
            _RefreshTokenExpiration = null;
            _RefreshTokenValidationCount = 0;
        }

        public void setRefreshToken(string refreshToken)
        {
            _RefreshToken = refreshToken;
            _RefreshTokenExpiration = null;
            _RefreshTokenValidationCount = 0;
        }

        public DateTime? getRefreshExpiration() => _RefreshTokenExpiration;

    }

}
