using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Provides hashing and verification utilities for user passwords used in authentication.
    /// </summary>
    public static class UserPasswordHasher
    {
        private static readonly PasswordHasher<UserLogin> _Hasher;

        static UserPasswordHasher()
        {
            var opt = new PasswordHasherOptions() { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3, IterationCount = 10000 };
            _Hasher = new PasswordHasher<UserLogin>(Options.Create(opt));

        }

        /// <summary>
        /// Verifies a password against a stored hash.
        /// </summary>
        /// <param name="user">User context used for salting.</param>
        /// <param name="hashed_password">Existing hashed password.</param>
        /// <param name="password">Plain text password to verify.</param>
        /// <returns>The result of the verification process.</returns>
        public static PasswordVerificationResult VerifyPassword(UserLogin user, string hashed_password, string password)
        {
            return _Hasher.VerifyHashedPassword(user, hashed_password, password);
        }

        /// <summary>
        /// Generates a hashed password for the specified user.
        /// </summary>
        /// <param name="user">User context used for salting.</param>
        /// <param name="password">Plain text password to hash.</param>
        /// <returns>The hashed password.</returns>
        public static string HashPassword(UserLogin user, string password)
        {
            return _Hasher.HashPassword(user, password);
        }

    }
}
