# Class: MicroM.Core.X509Encryptor
## Overview
Provides encryption and decryption services using an X509 certificate.

**Inheritance**
object -> X509Encryptor

**Implements**
IDisposable, IMicroMEncryption

## Example Usage
```csharp
using var enc = new MicroM.Core.X509Encryptor("thumbprint");
var encrypted = enc.Encrypt("secret");
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| X509Encryptor(string? certificate_thumbprint = null, string? certificate_name = null) | Initializes a new instance using the specified certificate. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Certificate | X509Certificate2? | Loaded certificate instance. |
| CertificateThumbprint | string? | Thumbprint of the loaded certificate. |

## Methods
| Method | Description |
|:------------|:-------------|
| Decrypt(string base64_encrypted) | Decrypts a base64 encrypted string. |
| Encrypt(string plaintext) | Encrypts text and returns a base64 string. |
| EncryptObject<T>(T obj) | Encrypts an object and returns a base64 string. |
| DecryptObject<T>(string encryptedString) | Decrypts an object from a base64 string. |
| Dispose() | Releases resources used by the encryptor. |

## Remarks
None.

