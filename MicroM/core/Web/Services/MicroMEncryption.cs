using MicroM.Configuration;
using MicroM.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicroM.Web.Services
{
    public class MicroMEncryption : IMicroMEncryption, IDisposable
    {
        private readonly X509Encryptor? _encryptor;
        private readonly ILogger<MicroMEncryption> _log;
        private bool disposedValue;

        public MicroMEncryption(IOptions<MicroMOptions> options, ILogger<MicroMEncryption> log)
        {
            _log = log;
            var thumbprint = options.Value.CertificateThumbprint;
            if (thumbprint == null)
            {
                _log.LogWarning("Certificate thumbprint not configured. Trying to read default certificate.");
                try
                {
                    _encryptor = new(certificate_name: ConfigurationDefaults.CertificateSubjectName);

                }
                catch
                {
                    _log.LogWarning("Default Certificate with name {certificate} not found in user store.", ConfigurationDefaults.CertificateSubjectName);
                }

            }
            else
            {
                try
                {
                    _encryptor = new(thumbprint);

                }
                catch
                {
                    _log.LogWarning("Certificate thumbprint {thumbprint} not found.", thumbprint);
                }
            }

            if (_encryptor == null)
            {
                // Create default certificate as we need to be able to encrypt always
                _log.LogWarning("Creating default certificate {name}.", ConfigurationDefaults.CertificateSubjectName);
                string password = CryptClass.CreateRandomPassword();
                using var cert = CryptClass.CreateSelfSignedCertificateAndStoreInUser(password);
                if (cert != null)
                {
                    _encryptor = new(cert.Thumbprint);
                }
            }
        }

        public string? CertificateThumbprint => _encryptor?.CertificateThumbprint;

        public string Decrypt(string base64_encrypted)
        {
            if (string.IsNullOrEmpty(base64_encrypted)) return base64_encrypted;
            return _encryptor?.Decrypt(base64_encrypted) ?? throw new InvalidOperationException("Certificate thumbprint not configured");
        }

        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;
            return _encryptor?.Encrypt(plaintext) ?? throw new InvalidOperationException("Certificate thumbprint not configured");
        }

        public string EncryptObject<T>(T obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return _encryptor?.EncryptObject(obj) ?? throw new InvalidOperationException("Certificate thumbprint not configured");
        }

        public T? DecryptObject<T>(string encryptedString)
        {
            if (string.IsNullOrEmpty(encryptedString)) throw new ArgumentException("Encrypted string cannot be null or empty.", nameof(encryptedString));
            if(_encryptor == null) throw new InvalidOperationException("Certificate thumbprint not configured");
            return _encryptor.DecryptObject<T>(encryptedString);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _encryptor?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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
