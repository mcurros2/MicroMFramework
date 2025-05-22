using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;

namespace LibraryTest
{
    internal class LocalEncryptor : IMicroMEncryption
    {
        readonly X509Encryptor _encryptor;

        public LocalEncryptor(string certificate_name = ConfigurationDefaults.CertificateSubjectName)
        {
            _encryptor = new(certificate_name: certificate_name);
        }

        public string? CertificateThumbprint => _encryptor.CertificateThumbprint;

        public string Decrypt(string base64_encrypted)
        {
            return _encryptor.Decrypt(base64_encrypted);
        }

        public string Encrypt(string plaintext)
        {
            return _encryptor.Encrypt(plaintext);
        }

        public string EncryptObject<T>(T obj)
        {
            return _encryptor.EncryptObject(obj);
        }

        public T? DecryptObject<T>(string encryptedString)
        {
            return _encryptor.DecryptObject<T>(encryptedString);
        }

    }
}
