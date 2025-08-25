using System;
using MicroM.Web.Services;
using System.Security.Cryptography.X509Certificates;

namespace MicroM.Core
{
    /// <summary>
    /// Provides encryption and decryption services using an X509 certificate.
    /// </summary>
    public class X509Encryptor : IDisposable, IMicroMEncryption
    {
        private readonly X509Certificate2? _certificate;
        private bool disposedValue;

        /// <summary>
        /// Gets the loaded certificate.
        /// </summary>
        public X509Certificate2? Certificate => _certificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="X509Encryptor"/> class.
        /// </summary>
        /// <param name="certificate_thumbprint">Certificate thumbprint to load.</param>
        /// <param name="certificate_name">Certificate subject name to load.</param>
        /// <exception cref="ArgumentException">Thrown when the certificate cannot be found.</exception>
        public X509Encryptor(string? certificate_thumbprint = null, string? certificate_name = null)
        {
            if (certificate_thumbprint != null)
            {
                _certificate = CryptClass.FindCertificate(certificate_thumbprint) ?? throw new ArgumentException("Certificate not found", nameof(certificate_thumbprint));
            }
            else if (certificate_name != null)
            {
                _certificate = CryptClass.FindCertificateByName(certificate_name) ?? throw new ArgumentException("Certificate not found", nameof(certificate_name));
            }
        }

        /// <summary>
        /// Gets the thumbprint of the loaded certificate.
        /// </summary>
        public string? CertificateThumbprint => _certificate?.Thumbprint;

        /// <summary>
        /// Decrypts a base64 encrypted string.
        /// </summary>
        /// <param name="base64_encrypted">The encrypted base64 string.</param>
        /// <returns>The decrypted plain text.</returns>
        public string Decrypt(string base64_encrypted)
        {
            if (_certificate == null) throw new ArgumentException("Certificate not configured");
            if (string.IsNullOrEmpty(base64_encrypted)) throw new ArgumentException("Empty base64 encrypted text", nameof(base64_encrypted));
            return CryptClass.DecryptObject<string>(base64_encrypted, _certificate) ?? throw new ArgumentException("Invalid base64 encrypted text", nameof(base64_encrypted));
        }

        /// <summary>
        /// Encrypts plain text and returns a base64 string.
        /// </summary>
        /// <param name="plaintext">The text to encrypt.</param>
        /// <returns>Base64 encoded encrypted string.</returns>
        public string Encrypt(string plaintext)
        {
            if (_certificate == null) throw new ArgumentException("Certificate not configured");
            if (string.IsNullOrEmpty(plaintext)) throw new ArgumentException("Empty text to encrypt", nameof(plaintext));
            return CryptClass.EncryptObject<string>(plaintext, _certificate) ?? throw new ArgumentException("Invalid base64 encrypted text", nameof(plaintext));
        }

        /// <summary>
        /// Encrypts an object and returns a base64 string.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">Object to encrypt.</param>
        /// <returns>Base64 encoded encrypted representation of the object.</returns>
        public string EncryptObject<T>(T obj)
        {
            if (_certificate == null) throw new ArgumentException("Certificate not configured");
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return CryptClass.EncryptObject<T>(obj, _certificate) ?? throw new ArgumentException("Invalid base64 encrypted text", nameof(obj));
        }

        /// <summary>
        /// Decrypts an encrypted object from a base64 string.
        /// </summary>
        /// <typeparam name="T">The type of object to decrypt.</typeparam>
        /// <param name="encryptedString">Encrypted base64 string.</param>
        /// <returns>The decrypted object or <c>null</c> if decryption fails.</returns>
        public T? DecryptObject<T>(string encryptedString)
        {
            if (_certificate == null) throw new ArgumentException("Certificate not configured");
            if (string.IsNullOrEmpty(encryptedString)) throw new ArgumentException("Empty base64 encrypted text", nameof(encryptedString));
            return CryptClass.DecryptObject<T>(encryptedString, _certificate) ?? throw new ArgumentException("Invalid base64 encrypted text", nameof(encryptedString));
        }

        /// <summary>
        /// Releases resources used by the instance.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from <see cref="Dispose()"/>.</param>
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

        /// <summary>
        /// Disposes the encryptor instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
