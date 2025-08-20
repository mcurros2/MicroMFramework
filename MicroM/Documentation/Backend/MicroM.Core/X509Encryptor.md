# Class: MicroM.Core.X509Encryptor

## Overview
Implements `IMicroMEncryption` using an X509 certificate to encrypt and decrypt data.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| X509Encryptor(string? certificate_thumbprint = null, string? certificate_name = null) | Loads a certificate by thumbprint or subject name. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Certificate | X509Certificate2? | Loaded certificate instance. |
| CertificateThumbprint | string? | Thumbprint of the loaded certificate. |

## Methods
| Method | Description |
|:------------|:-------------|
| Encrypt(string plaintext) | Encrypts text using the certificate. |
| Decrypt(string base64_encrypted) | Decrypts Base64 text using the certificate. |
| EncryptObject<T>(T obj) | Serializes and encrypts an object. |
| DecryptObject<T>(string encryptedString) | Decrypts and deserializes an object. |

## Remarks
Wraps `CryptClass` helpers to provide certificate-based encryption services.

## See Also
- [CryptClass](CryptClass.md)
