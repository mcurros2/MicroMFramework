using MicroM.Configuration;
using MicroM.Core;
using MicroM.Web.Services;
using System.Security.Cryptography.X509Certificates;

namespace LibraryTest
{
    internal class LocalEncryptor : IMicroMEncryption
    {
        readonly X509Encryptor _encryptor;
        public LocalEncryptor(string certificate_name = ConfigurationDefaults.CertificateSubjectName)
        {
            _encryptor = new(certificate_name: certificate_name);
        }
        public string Decrypt(string base64_encrypted)
        {
            return _encryptor.Decrypt(base64_encrypted);
        }

        public string Encrypt(string plaintext)
        {
            return _encryptor.Encrypt(plaintext);
        }

        public X509Certificate2 GetCertificate()
        {
            return _encryptor?.Certificate;
        }
    }
}
