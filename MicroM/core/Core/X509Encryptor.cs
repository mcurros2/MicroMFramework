using MicroM.Web.Services;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Core
{
    public class X509Encryptor : IDisposable, IMicroMEncryption
    {
        private readonly X509Certificate2? _certificate;
        private bool disposedValue;

        public X509Certificate2? Certificate => _certificate;

        /// <summary>
        /// Provides certificate encryption.
        /// </summary>
        /// <param name="certificate_thumbprint"></param>
        /// <exception cref="ArgumentException"></exception>
        public X509Encryptor(string? certificate_thumbprint = null, string? certificate_name = null)
        {
            if(certificate_thumbprint != null)
            {
                _certificate = CryptClass.FindCertificate(certificate_thumbprint) ?? throw new ArgumentException($"Certificate not found", nameof(certificate_thumbprint));
            }
            else if(certificate_name != null)
            {
                _certificate = CryptClass.FindCertificateByName(certificate_name) ?? throw new ArgumentException($"Certificate not found", nameof(certificate_name));
            }
            
        }

        public string? CertificateThumbprint => _certificate?.Thumbprint;

        public string Decrypt(string base64_encrypted)
        {
            if(_certificate == null) throw new ArgumentException("Certificate not configured");
            if (string.IsNullOrEmpty(base64_encrypted)) throw new ArgumentException($"Empty base64 encrypted text", nameof(base64_encrypted));
            return CryptClass.DecryptObject<string>(base64_encrypted, _certificate) ?? throw new ArgumentException($"Invalid base64 encrypted text", nameof(base64_encrypted));
        }

        public string Encrypt(string plaintext)
        {
            if (_certificate == null) throw new ArgumentException("Certificate not configured");
            if (string.IsNullOrEmpty(plaintext)) throw new ArgumentException($"Empty text to encrypt", nameof(plaintext));
            return CryptClass.EncryptObject<string>(plaintext, _certificate) ?? throw new ArgumentException($"Invalid base64 encrypted text", nameof(plaintext));
        }

        public string EncryptObject<T>(T obj)
        {
            if (_certificate == null) throw new ArgumentException("Certificate not configured");
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return CryptClass.EncryptObject<T>(obj, _certificate) ?? throw new ArgumentException($"Invalid base64 encrypted text", nameof(obj));
        }

        public T? DecryptObject<T>(string encryptedString)
        {
            if (_certificate == null) throw new ArgumentException("Certificate not configured");
            if (string.IsNullOrEmpty(encryptedString)) throw new ArgumentException($"Empty base64 encrypted text", nameof(encryptedString));
            return CryptClass.DecryptObject<T>(encryptedString, _certificate) ?? throw new ArgumentException($"Invalid base64 encrypted text", nameof(encryptedString));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _certificate?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
