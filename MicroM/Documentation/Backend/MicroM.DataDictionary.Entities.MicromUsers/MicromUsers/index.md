# Class: MicroM.DataDictionary.MicromUsers
## Overview
Entity for interacting with MicroM user records.

**Inheritance**
Entity<MicromUsersDef> -> MicromUsers

## Constructors
| Constructor | Description |
|:------------|:-------------|
| MicromUsers() | Initializes a new instance of the MicromUsers class. |
| MicromUsers(IEntityClient, IMicroMEncryption?) | Initializes a new instance with a database client and optional encryptor. |

## Methods
| Method | Description |
|:------------|:-------------|
| Logoff | Logs off the specified user. |
| GetUserData | Retrieves user data for the given identifiers. |
| GetClaims | Retrieves server and client claims. |
| UpdateLoginAttempt | Updates login attempt information. |
| RefreshToken | Refreshes a user's token. |
| GetRecoveryCode | Obtains a recovery code for the user. |
| GetRecoveryEmails | Retrieves recovery email addresses. |
| RecoverPassword | Recovers a user's password using a recovery code. |
| usr_setPassword | Sets a new password hash for the user. |
| usr_resetPassword | Resets a user's password to a random value. |

## See Also
- [MicromUsersDef](../MicromUsersDef/index.md)
- [MicromUsersDevices](../MicromUsersDevices/index.md)

