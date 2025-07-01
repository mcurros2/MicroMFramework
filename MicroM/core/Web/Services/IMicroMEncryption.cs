namespace MicroM.Web.Services
{
    public interface IMicroMEncryption
    {
        public string Decrypt(string base64_encrypted);

        public string Encrypt(string plaintext);

        public string EncryptObject<T>(T obj);

        public T? DecryptObject<T>(string encryptedString);

        public string? CertificateThumbprint { get; }
    }
}
