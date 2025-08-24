using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication
{
    /// <summary>
    /// Represents the UserPasswordHasher.
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
        /// Performs the VerifyPassword operation.
        /// </summary>
        public static PasswordVerificationResult VerifyPassword(UserLogin user, string hashed_password, string password)
        {
            return _Hasher.VerifyHashedPassword(user, hashed_password, password);
        }

        /// <summary>
        /// Performs the HashPassword operation.
        /// </summary>
        public static string HashPassword(UserLogin user, string password)
        {
            return _Hasher.HashPassword(user, password);
        }

    }
}
