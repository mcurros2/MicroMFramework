# Interface: MicroM.Web.Authentication.SSO.IIdentityProviderService
## Overview
Defines SSO interactions with identity providers.

## Methods
| Method | Description |
|:------------|:-------------|
| GetWellKnown | Retrieves the OpenID Connect discovery document. |
| GetJwks | Retrieves the JSON Web Key Set. |
| GetAuthorizationCode | Requests an authorization code for the specified user. |
| GetBackchannelToken | Requests a backchannel token for the specified user. |

## Remarks

## See Also
- [MicroM.Web.Authentication.SSO](./index.md)
