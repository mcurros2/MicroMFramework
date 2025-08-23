namespace MicroM.Configuration
{
    /// <summary>
    /// Stores transient cryptographic values used by the framework. These
    /// defaults are generated at runtime and remain valid only for the
    /// lifetime of the application instance.
    /// </summary>
    public sealed class SecurityDefaults
    {
        /// <summary>
        /// Temporary initialization vector valid for the lifetime of the application.
        /// </summary>
        internal static byte[] TempEncryptionIV = null!;
        /// <summary>
        /// Temporary encryption key valid for the lifetime of the application.
        /// </summary>
        internal static byte[] TempEncryptionKey = null!;
    }
}

