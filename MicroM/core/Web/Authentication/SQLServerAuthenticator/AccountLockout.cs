using MicroM.DataDictionary.Entities.MicromUsers;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Tracks failed login attempts, lockout expiration, and refresh token
    /// validation for a single account.
    /// </summary>
    /// <remarks>
    /// Instances of this class are cached by <see cref="SQLServerAuthenticator"/>
    /// to preserve lockout state between authentication requests.
    /// </remarks>
    public class AccountLockout
    {
        /// <summary>
        /// Number of consecutive failed logon attempts. When this value exceeds
        /// the threshold, the account is locked for a period of time.
        /// </summary>
        int _badlogon_attempts = 0;

        /// <summary>
        /// Point in time when the current lockout expires. A <see langword="null"/>
        /// value indicates the account is not locked.
        /// </summary>
        DateTime? _locked_until = null;

        /// <summary>
        /// Refresh token issued for the account.
        /// </summary>
        string? _RefreshToken = null;

        /// <summary>
        /// Expiration time for the current refresh token.
        /// </summary>
        DateTime? _RefreshTokenExpiration = null;

        /// <summary>
        /// Number of times the refresh token has been validated.
        /// </summary>
        int _RefreshTokenValidationCount = 0;

        /// <summary>
        /// Determines whether the account is currently locked.
        /// </summary>
        public bool isAccountLocked()
        {
            if (_locked_until == null) return false;
            if (_locked_until < DateTime.Now) return true;
            return false;
        }

        /// <summary>
        /// Clears the lockout state and resets the failed logon counter.
        /// </summary>
        public void unlockAccount()
        {
            _locked_until = null;
            _badlogon_attempts = 0;
        }

        /// <summary>
        /// Increments the failed logon counter and locks the account when the
        /// configured threshold is exceeded.
        /// </summary>
        /// <remarks>
        /// After more than ten consecutive failures the account is locked for
        /// fifteen minutes.
        /// </remarks>
        public void incrementBadLogonAndLock()
        {
            _badlogon_attempts++;
            if (_badlogon_attempts > 10)
            {
                _locked_until = DateTime.Now.AddMinutes(15);
            }
        }

        /// <summary>
        /// Validates a refresh token against expiration and usage limits.
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
        /// Increments the number of refresh token validations.
        /// </summary>
        public void incrementRefreshTokenValidationCount()
        {
            _RefreshTokenValidationCount++;
        }

        /// <summary>
        /// Clears the refresh token and associated validation state.
        /// </summary>
        public void clearRefreshToken()
        {
            _RefreshToken = null;
            _RefreshTokenExpiration = null;
            _RefreshTokenValidationCount = 0;
        }

        /// <summary>
        /// Sets a new refresh token and resets its validation state.
        /// </summary>
        public void setRefreshToken(string refreshToken)
        {
            _RefreshToken = refreshToken;
            _RefreshTokenExpiration = null;
            _RefreshTokenValidationCount = 0;
        }

        /// <summary>
        /// Gets the expiration time for the current refresh token.
        /// </summary>
        public DateTime? getRefreshExpiration() => _RefreshTokenExpiration;

    }

}
