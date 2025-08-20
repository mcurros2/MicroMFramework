# Class: MicroM.Core.CryptClass

## Overview
Provides helper methods for symmetric encryption, certificate creation, and random data generation.

## Methods
| Method | Description |
|:------------|:-------------|
| TempEncryptString(string to_encrypt) | Encrypts text using a temporary AES key. |
| GetSecurityKey(string password, string salt, int keySize = 256) | Derives a symmetric security key using PBKDF2. |
| EncryptText(string text, byte[] key, byte[] iv) | Encrypts text with the provided AES key and IV. |
| CreateSelfSignedCertificate(string distinguished_name, int expires_years = 50) | Generates a self-signed certificate. |
| ExportCertificate(X509Certificate2 cert, string path, string password, CancellationToken ct) | Exports a certificate to a PFX file. |
| StoreCertificate(X509Certificate2 certificate, string password) | Stores a certificate in the current user's store. |
| CreateSelfSignedCertificateAndStoreInUser(string password, string name, int expires_years = 50) | Creates and stores a self-signed certificate. |
| DeleteCertificate(string subject_name) | Removes a certificate from the current user's store. |
| FindCertificateByName(string subject_name) | Retrieves a certificate by subject name. |
| FindCertificate(string thumbprint) | Retrieves a certificate by thumbprint. |
| X509Encrypt(string plainText, X509Certificate2 cert) | Encrypts text using an X509 certificate. |
| X509Decrypt(string encryptedText, X509Certificate2 cert) | Decrypts text using an X509 certificate. |
| GenerateRandomBase64String(int count = 8) | Produces a random Base64 string. |
| CreateRandomPassword(int length = 50, ...) | Generates a random password meeting complexity requirements. |

## Remarks
Central location for cryptographic helpers used throughout MicroM.

## See Also
- [X509Encryptor](X509Encryptor.md)
