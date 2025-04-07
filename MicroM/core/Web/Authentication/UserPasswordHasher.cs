using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Authentication
{
    public static class UserPasswordHasher
    {
        private static readonly PasswordHasher<UserLogin> _Hasher;

        static UserPasswordHasher()
        {
            var opt = new PasswordHasherOptions() { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3, IterationCount = 10000 };
            _Hasher = new PasswordHasher<UserLogin>(Options.Create(opt));

        }

        public static PasswordVerificationResult VerifyPassword(UserLogin user, string hashed_password, string password)
        {
            return _Hasher.VerifyHashedPassword(user, hashed_password, password);
        }

        public static string HashPassword(UserLogin user, string password)
        {
            return _Hasher.HashPassword(user, password);
        }

    }
}
