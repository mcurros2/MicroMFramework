# Record: MicroM.DataDictionary.Entities.MicromUsers.LoginAttemptResult

## Overview
Represents the outcome of updating a user login attempt.

## Properties
| Property | Description |
|:--|:--|
| Status | Resulting [LoginAttemptStatus](LoginAttemptStatus.md) of the attempt. |
| Message | Optional status message. |
| RefreshToken | New refresh token when applicable. |

## Remarks
Returned after invoking `usr_updateLoginAttempt` to track lockouts and refresh token issuance.
