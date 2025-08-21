# Class: MicroM.Core.CryptClass
## Overview
Cryptographic helper utilities.

**Inheritance**
object -> CryptClass

**Implements**
None

## Example Usage
```csharp
var key = CryptClass.GenerateRandomBase64String();
```
## Methods
| Method | Description |
|:------------|:-------------|
| TempEncryptString(string to_encrypt) | Encrypts a string using a temporary AES key. |
| GetSecurityKey(string password, string salt, int keySize) | Derives a symmetric security key. |
| EncryptText(string text, byte[] key, byte[] iv) | Encrypts text with AES. |
| CreateSelfSignedCertificate(string distinguished_name, int expires_years) | Creates a self-signed certificate. |
| ExportCertificate(X509Certificate2 cert, string certificate_full_path, string export_password, CancellationToken ct) | Exports a certificate to a file. |
| StoreCertificate(X509Certificate2 certificate, string certificate_password) | Stores a certificate in the user's store. |
| CreateSelfSignedCertificateAndStoreInUser(string certificate_password, string distinguished_name, int expires_years) | Creates and stores a certificate. |
| DeleteCertificate(string subject_name) | Deletes a certificate from the user store. |
| FindCertificateByName(string subject_name) | Finds a certificate by subject name. |
| FindCertificate(string thumbprint) | Finds a certificate by thumbprint. |
| X509Encrypt(string plainText, X509Certificate2 cert) | Encrypts text using an X509 certificate. |
| X509Decrypt(string encryptedText, X509Certificate2 cert) | Decrypts text using an X509 certificate. |
| GenerateRandomBase64String(int count) | Generates random base64 data. |
| CreateRandomPassword(int length, int minSymbols, int minNumbers, int minUppercase, int minLowercase) | Creates a random password. |
| EncryptObject<T>(T obj, X509Certificate2 cert) | Encrypts an object and returns a base64 string. |
| DecryptObject<T>(string encryptedString, X509Certificate2 cert) | Decrypts an object from a base64 string. |

## Remarks
None.

## See Also
-
