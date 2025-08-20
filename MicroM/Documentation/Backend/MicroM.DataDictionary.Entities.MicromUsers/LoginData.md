# Record: MicroM.DataDictionary.Entities.MicromUsers.LoginData

## Overview
Lightweight data structure returned when retrieving user information during login.

## Properties
| Property | Description |
|:--|:--|
| user_id | Identifier of the user. |
| locked | Indicates if the account is locked. |
| pwhash | Stored password hash. |
| badlogonattempts | Count of failed login attempts. |
| username | User name. |
| disabled | True if the account is disabled. |
| refresh_token | Current refresh token value. |
| refresh_expired | Indicates if refresh token has expired. |
| user_groups | JSON string of group identifiers. |

## Remarks
Used by authentication workflows to check status and lockouts.
