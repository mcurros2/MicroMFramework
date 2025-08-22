using MicroM.DataDictionary.Entities.MicromUsers;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents the AccountLockout.
    /// </summary>
    public class AccountLockout
    {
        int _badlogon_attempts = 0;
        DateTime? _locked_until = null;

        string? _RefreshToken = null;
        DateTime? _RefreshTokenExpiration = null;
        int _RefreshTokenValidationCount = 0;

        /// <summary>
        /// Performs the isAccountLocked operation.
        /// </summary>
        public bool isAccountLocked()
        {
            if (_locked_until == null) return false;
            if (_locked_until < DateTime.Now) return true;
            return false;
        }

        /// <summary>
        /// Performs the unlockAccount operation.
        /// </summary>
        public void unlockAccount()
        {
            _locked_until = null;
            _badlogon_attempts = 0;
        }

        /// <summary>
        /// Performs the incrementBadLogonAndLock operation.
        /// </summary>
        public void incrementBadLogonAndLock()
        {
            _badlogon_attempts++;
            if (_badlogon_attempts > 10)
            {
                _locked_until = DateTime.Now.AddMinutes(15);
            }
        }

        /// <summary>
        /// Performs the validateRefreshToken operation.
        /// </summary>
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

        /// <summary>
        /// Performs the incrementRefreshTokenValidationCount operation.
        /// </summary>
        public void incrementRefreshTokenValidationCount()
        {
            _RefreshTokenValidationCount++;
        }

        /// <summary>
        /// Performs the clearRefreshToken operation.
        /// </summary>
        public void clearRefreshToken()
        {
            _RefreshToken = null;
            _RefreshTokenExpiration = null;
            _RefreshTokenValidationCount = 0;
        }

        /// <summary>
        /// Performs the setRefreshToken operation.
        /// </summary>
        public void setRefreshToken(string refreshToken)
        {
            _RefreshToken = refreshToken;
            _RefreshTokenExpiration = null;
            _RefreshTokenValidationCount = 0;
        }

        /// <summary>
        /// Performs the getRefreshExpiration operation.
        /// </summary>
        public DateTime? getRefreshExpiration() => _RefreshTokenExpiration;

    }

}
