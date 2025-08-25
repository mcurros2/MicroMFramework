# Class: MicroM.DataDictionary.MicromUsers
## Overview
Entity for interacting with MicroM user records.

**Inheritance**
Entity<MicromUsersDef> -> MicromUsers

## Constructors
| Constructor | Description |
|:------------|:-------------|
| MicromUsers() | Default constructor. |
| MicromUsers(IEntityClient, IMicroMEncryption?) | Creates a MicromUsers entity with the specified client and encryptor. |

## Methods
| Method | Description |
|:------------|:-------------|
| InsertData(CancellationToken, bool, MicroMOptions?, Dictionary<string, object>?, IWebAPIServices?, string?) | Inserts the user data hashing the password before saving. |
| Logoff(string, IEntityClient, CancellationToken) | Logs off the specified user. |
| GetUserData(string?, string?, string, IEntityClient, CancellationToken) | Retrieves user data for the given identifiers. |
| GetClaims(string, IEntityClient, CancellationToken) | Retrieves both server and client claims for a user. |
| UpdateLoginAttempt(string, string, string?, bool, int, int, int, string, string, IEntityClient, CancellationToken) | Updates login attempt information and returns the result. |
| RefreshToken(string, string, string, string, int, int, IEntityClient, CancellationToken) | Refreshes a user\'s token and returns the new token information. |
| GetRecoveryCode(string, IEntityClient, CancellationToken) | Obtains a recovery code for the specified user. |
| GetRecoveryEmails(string, IEntityClient, CancellationToken) | Retrieves recovery email addresses for the user. |
| RecoverPassword(string, string, string, IEntityClient, CancellationToken) | Recovers a user\'s password using a recovery code. |
| usr_setPassword(string, string, IEntityClient, CancellationToken) | Sets a new password hash for the user. |
| usr_resetPassword(string, IEntityClient, CancellationToken) | Resets a user\'s password to a random value. |

## See Also
- [MicromUsersDef](../MicromUsersDef/index.md)
