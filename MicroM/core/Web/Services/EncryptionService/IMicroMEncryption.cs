namespace MicroM.Web.Services
{
    /// <summary>
    /// Represents the IMicroMEncryption.
    /// </summary>
    public interface IMicroMEncryption
    {
        /// <summary>
        /// Performs the Decrypt operation.
        /// </summary>
        public string Decrypt(string base64_encrypted);

        /// <summary>
        /// Performs the Encrypt operation.
        /// </summary>
        public string Encrypt(string plaintext);

        /// <summary>
        /// Performs the EncryptObject<T> operation.
        /// </summary>
        public string EncryptObject<T>(T obj);

        /// <summary>
        /// Performs the DecryptObject<T> operation.
        /// </summary>
        public T? DecryptObject<T>(string encryptedString);

        /// <summary>
        /// Gets or sets the }.
        /// </summary>
        public string? CertificateThumbprint { get; }
    }
}
