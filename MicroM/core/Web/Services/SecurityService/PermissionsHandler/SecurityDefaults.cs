namespace MicroM.Configuration
{
    /// <summary>
    /// Represents the SecurityDefaults.
    /// </summary>
    public sealed class SecurityDefaults
    {
        /// <summary>
        /// Temporary encryption IV valid for the lifetime of the app
        /// </summary>
        internal static byte[] TempEncryptionIV = null!;
        /// <summary>
        /// Temporary encryption key for the lifetime of the app
        /// </summary>
        internal static byte[] TempEncryptionKey = null!;
    }
}
