# Class: MicroM.DataDictionary.MicromUsersDef
## Overview
Schema definition for MicroM user records.

**Inheritance**
EntityDefinition -> MicromUsersDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| MicromUsersDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_user_id | Column<string> | Primary identifier of the user. |
| vc_username | Column<string> | Username used for authentication. |
| vc_email | Column<string?> | User email address. |
| vc_pwhash | Column<string> | Password hash value. |
| vb_sid | Column<string?> | Security identifier. |
| i_badlogonattempts | Column<int> | Failed login attempt count. |
| bt_disabled | Column<bool> | Indicates whether the user is disabled. |
| dt_locked | Column<DateTime?> | Lockout expiration time. |
| dt_last_login | Column<DateTime?> | Timestamp of the last successful login. |
| dt_last_refresh | Column<DateTime?> | Timestamp of the last refresh token. |
| vc_recovery_code | Column<string?> | Recovery code for password reset. |
| dt_last_recovery | Column<DateTime?> | Time when recovery code was last generated. |
| c_usertype_id | Column<string> | User type identifier. |
| vc_user_groups | Column<string[]?> | User group memberships. |
| bt_islocked | Column<bool> | Indicates whether the user is locked. |
| i_locked_minutes_remaining | Column<int> | Minutes remaining until unlock. |
| vc_password | Column<string> | Plain text password used during creation. |
| usr_brwStandard | ViewDefinition | Default browse view definition. |
| usr_getUserData | ProcedureDefinition | Procedure to get user data. |
| usr_updateLoginAttempt | ProcedureDefinition | Procedure to update login attempt data. |
| usr_logoff | ProcedureDefinition | Procedure to log off a user. |
| usr_setPassword | ProcedureDefinition | Procedure to set a user password. |
| usr_resetPassword | ProcedureDefinition | Procedure to reset a user password. |
| usr_GetClientClaims | ProcedureDefinition | Procedure to retrieve client claims. |
| usr_GetServerClaims | ProcedureDefinition | Procedure to retrieve server claims. |
| usr_GetEnabledMenus | ProcedureDefinition | Procedure to retrieve enabled menus. |
| usr_GetRecoveryCode | ProcedureDefinition | Procedure to generate a recovery code. |
| usr_GetRecoveryEmails | ProcedureDefinition | Procedure to get recovery email addresses. |
| usr_RecoverPassword | ProcedureDefinition | Procedure to recover a password. |
| UNUsername | EntityUniqueConstraint | Unique constraint ensuring usernames are unique. |
| FKGroups | EntityForeignKey<MicromUsersGroups, MicromUsers> | Relationship to the groups associated with the user. |

## See Also
- [MicromUsers](../MicromUsers/index.md)
