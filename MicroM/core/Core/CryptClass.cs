using MicroM.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace MicroM.Core
{
    /// <summary>
    /// Provides cryptographic helper methods.
    /// </summary>
    public class CryptClass
    {
        /// <summary>
        /// Temporarily encrypts a string using a random AES key.
        /// </summary>
        /// <param name="to_encrypt">Text to encrypt.</param>
        /// <returns>Encrypted bytes.</returns>
        public static async Task<byte[]> TempEncryptString(string to_encrypt)
        {
            using var aes = Aes.Create();
            SecurityDefaults.TempEncryptionKey ??= aes.Key;
            SecurityDefaults.TempEncryptionIV ??= aes.IV;

            return await EncryptText(to_encrypt, SecurityDefaults.TempEncryptionKey, SecurityDefaults.TempEncryptionIV);
        }

        /// <summary>
        /// Derives a symmetric security key from a password and salt.
        /// </summary>
        public static SymmetricSecurityKey GetSecurityKey(string password, string salt, int keySize = 256)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 1000, HashAlgorithmName.SHA512);
            var keyBytes = pbkdf2.GetBytes(keySize / 8);  // Divide by 8 to get byte count

            return new SymmetricSecurityKey(keyBytes);
        }


        /// <summary>
        /// Encrypts text using AES with the provided key and IV.
        /// </summary>
        public static async Task<byte[]> EncryptText(string text, byte[] key, byte[] iv)
        {

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using var crypt_stream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(crypt_stream);

            await sw.WriteAsync(text);

            return ms.ToArray();

        }

        /// <summary>
        /// Creates a self-signed X509 certificate.
        /// </summary>
        public static X509Certificate2 CreateSelfSignedCertificate(string distinguished_name = ConfigurationDefaults.CertificateSubjectName, int expires_years = 50)
        {
            using var rsa = RSA.Create(2048);

            var request = new CertificateRequest($"cn={distinguished_name}", rsa, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add
                (
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyAgreement, false)
                );

            // MMC: See https://access.redhat.com/documentation/en-us/red_hat_certificate_system/9/html/administration_guide/standard_x.509_v3_certificate_extensions
            // 1.3.6.1.5.5.7.3.1 = Server Authentication
            // 1.3.6.1.5.5.7.3.2 = Client Authentication
            request.CertificateExtensions.Add
                (
                new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1"), new Oid("1.3.6.1.5.5.7.3.2")], false)
                );

            // Create a self-signed certificate
            return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(expires_years));

        }

        /// <summary>
        /// Exports a certificate to a PFX file.
        /// </summary>
        public static async Task ExportCertificate(X509Certificate2 cert, string certificate_full_path, string export_password, CancellationToken ct)
        {
            if (!Directory.Exists(certificate_full_path)) Directory.CreateDirectory(certificate_full_path);
            await File.WriteAllBytesAsync(certificate_full_path, cert.Export(X509ContentType.Pkcs12, export_password), ct);
        }

        /// <summary>
        /// Stores a certificate in the current user's certificate store.
        /// </summary>
        public static X509Certificate2 StoreCertificate(X509Certificate2 certificate, string certificate_password)
        {
            // Export the certificate to a PFX file, to create with a password
            var pfx = certificate.Export(X509ContentType.Pkcs12, certificate_password);

            var exported_certificate = new X509Certificate2(pfx, certificate_password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            // Import the PFX file into the X509Store
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadWrite);
            store.Add(exported_certificate);
            store.Close();

            return exported_certificate;

        }

        /// <summary>
        /// Creates a self-signed certificate and stores it in the current user's store.
        /// </summary>
        public static X509Certificate2 CreateSelfSignedCertificateAndStoreInUser(string certificate_password, string distinguished_name = ConfigurationDefaults.CertificateSubjectName, int expires_years = 50)
        {

            using var certificate = CreateSelfSignedCertificate(distinguished_name, expires_years);
            return StoreCertificate(certificate, certificate_password);
        }


        /// <summary>
        /// Deletes a certificate by subject name from the current user's store.
        /// </summary>
        public static bool DeleteCertificate(string subject_name)
        {
            bool ret = false;

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadWrite);

            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, subject_name, false);
            if (certs != null && certs.Count > 0)
            {
                store.Remove(certs[0]);
                ret = true;
            }

            store.Close();

            return ret;
        }

        private static X509Certificate2? Find(X509FindType find_type, string name)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);

            var certs = store.Certificates.Find(find_type, name, false);

            store.Close();

            if (certs != null)
            {
                if (find_type == X509FindType.FindBySubjectName && !name.StartsWith("CN=", StringComparison.OrdinalIgnoreCase)) name = $"CN={name}";

                foreach (var cert in certs)
                {
                    if (find_type == X509FindType.FindBySubjectName && cert.Subject == name) return cert;
                    if (find_type == X509FindType.FindByThumbprint && cert.Thumbprint == name) return cert;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a certificate by subject name.
        /// </summary>
        public static X509Certificate2? FindCertificateByName(string subject_name)
        {
            return Find(X509FindType.FindBySubjectName, subject_name);

        }


        /// <summary>
        /// Finds a certificate by thumbprint.
        /// </summary>
        public static X509Certificate2? FindCertificate(string thumbprint)
        {
            return Find(X509FindType.FindByThumbprint, thumbprint);
        }

        /// <summary>
        /// Encrypts text using the public key of the certificate.
        /// </summary>
        public static string? X509Encrypt(string plainText, X509Certificate2 cert)
        {
            // Get the public key from the certificate
            var publicKey = cert.GetRSAPublicKey();

            // Encrypt the data using the public key
            var data = Encoding.UTF8.GetBytes(plainText);
            var encryptedData = publicKey?.Encrypt(data, RSAEncryptionPadding.OaepSHA256);

            // Return the encrypted data as a base64-encoded string
            return encryptedData == null ? null : Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Decrypts text using the private key of the certificate.
        /// </summary>
        public static string? X509Decrypt(string encryptedText, X509Certificate2 cert)
        {
            // Get the private key from the certificate
            var privateKey = cert.GetRSAPrivateKey();

            // Decrypt the data using the private key
            var encryptedData = Convert.FromBase64String(encryptedText);
            var decryptedData = privateKey?.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);

            // Return the decrypted data as a string
            return decryptedData == null ? null : Encoding.UTF8.GetString(decryptedData);
        }

        /// <summary>
        /// Generates a random base64 string of the specified byte length.
        /// </summary>
        public static string GenerateRandomBase64String(int count = 8)
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(count);

            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Creates a random password satisfying the specified requirements.
        /// </summary>
        public static string CreateRandomPassword(int length = 50, int minSymbols = 5, int minNumbers = 5, int minUppercase = 5, int minLowercase = 5)
        {
            ReadOnlySpan<char> symbols = "!@#$%^&*()_-+=[]{};:>|./?";
            ReadOnlySpan<char> numbers = "0123456789";
            ReadOnlySpan<char> uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            ReadOnlySpan<char> lowercase = "abcdefghijklmnopqrstuvwxyz";
            Span<char> allChars = stackalloc char[symbols.Length + numbers.Length + uppercase.Length + lowercase.Length];
            symbols.CopyTo(allChars);
            numbers.CopyTo(allChars[symbols.Length..]);
            uppercase.CopyTo(allChars[(symbols.Length + numbers.Length)..]);
            lowercase.CopyTo(allChars[(symbols.Length + numbers.Length + uppercase.Length)..]);

            if (length < minSymbols + minNumbers + minUppercase + minLowercase) length = minSymbols + minNumbers + minUppercase + minLowercase;

            Span<char> password = stackalloc char[length];

            // Add the minimum required characters in random order
            var requiredChars = GetRandomCharacters(symbols, minSymbols) +
                                GetRandomCharacters(numbers, minNumbers) +
                                GetRandomCharacters(uppercase, minUppercase) +
                                GetRandomCharacters(lowercase, minLowercase);

            requiredChars.AsSpan().CopyTo(password);

            // Add the remaining characters randomly
            for (int i = requiredChars.Length; i < length; i++)
            {
                password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
            }

            // Shuffle the characters
            return new string(Shuffle(password).ToArray());
        }

        private static string GetRandomCharacters(ReadOnlySpan<char> validChars, int count)
        {
            Span<char> res = stackalloc char[count];
            for (int i = 0; i < count; i++)
            {
                res[i] = validChars[RandomNumberGenerator.GetInt32(validChars.Length)];
            }
            return new string(res);
        }

        private static Span<T> Shuffle<T>(Span<T> span)
        {
            for (int n = span.Length - 1; n > 0; --n)
            {
                uint k = (uint)RandomNumberGenerator.GetInt32(n + 1);
                (span[n], span[(int)k]) = (span[(int)k], span[n]);
            }
            return span;
        }

        /// <summary>
        /// Encrypts an object using hybrid RSA/AES and returns a base64 string.
        /// </summary>
        public static string EncryptObject<T>(T obj, X509Certificate2 cert)
        {
            var rsaParams = (cert.GetRSAPublicKey()?.ExportParameters(false)) ?? throw new Exception("Could not get RSA parameters from certificate");

            // Serialize the object to JSON
            string json = JsonSerializer.Serialize(obj);

            // Create a random AES key
            using var aes = Aes.Create();

            // Create a 12-byte nonce (IV)
            byte[] iv = new byte[12];
            RandomNumberGenerator.Fill(iv);

            // Encrypt the AES key with RSA
            byte[] encryptedAesKey;
            using var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(rsaParams);
            encryptedAesKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);

            // MMC: tagsize = 128 bits = 16 bytes
            using var aesgcm = new AesGcm(aes.Key, 16);

            byte[] buffer = Encoding.UTF8.GetBytes(json);
            byte[] encryptedBuffer = new byte[buffer.Length];
            byte[] tag = new byte[16];

            aesgcm.Encrypt(iv, buffer, encryptedBuffer, tag);

            // Concatenate encrypted AES key, IV, encrypted buffer and tag into one byte array
            byte[] result = new byte[encryptedAesKey.Length + iv.Length + encryptedBuffer.Length + tag.Length];
            Buffer.BlockCopy(encryptedAesKey, 0, result, 0, encryptedAesKey.Length);
            Buffer.BlockCopy(iv, 0, result, encryptedAesKey.Length, iv.Length);
            Buffer.BlockCopy(encryptedBuffer, 0, result, encryptedAesKey.Length + iv.Length, encryptedBuffer.Length);
            Buffer.BlockCopy(tag, 0, result, encryptedAesKey.Length + iv.Length + encryptedBuffer.Length, tag.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a previously encrypted object.
        /// </summary>
        public static T? DecryptObject<T>(string encryptedString, X509Certificate2 cert)
        {
            byte[] data = Convert.FromBase64String(encryptedString);

            using var pub_key = cert.GetRSAPublicKey() ?? throw new Exception("Could not get RSA public key from certificate");
            using var priv_key = cert.GetRSAPrivateKey() ?? throw new Exception("Could not get RSA private key from certificate");

            byte[] encryptedAesKey = new byte[pub_key.KeySize / 8]; // RSA key length
            byte[] IV = new byte[12]; // GCM standard IV length
            byte[] encryptedBuffer = new byte[data.Length - encryptedAesKey.Length - IV.Length - 16]; // 16 bytes for GCM tag (128 bits)
            byte[] tag = new byte[16];

            Buffer.BlockCopy(data, 0, encryptedAesKey, 0, encryptedAesKey.Length);
            Buffer.BlockCopy(data, encryptedAesKey.Length, IV, 0, IV.Length);
            Buffer.BlockCopy(data, encryptedAesKey.Length + IV.Length, encryptedBuffer, 0, encryptedBuffer.Length);
            Buffer.BlockCopy(data, encryptedAesKey.Length + IV.Length + encryptedBuffer.Length, tag, 0, tag.Length);

            // Decrypt the AES key
            byte[] aesKey;

            aesKey = priv_key.Decrypt(encryptedAesKey, RSAEncryptionPadding.Pkcs1);

            byte[] buffer = new byte[encryptedBuffer.Length];

            using var aesgcm = new AesGcm(aesKey, 16);

            aesgcm.Decrypt(IV, encryptedBuffer, tag, buffer);

            string json = Encoding.UTF8.GetString(buffer);
            return JsonSerializer.Deserialize<T>(json);
        }


    }
}
