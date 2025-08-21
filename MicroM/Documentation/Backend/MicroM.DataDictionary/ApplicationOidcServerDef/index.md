# Class: MicroM.DataDictionary.ApplicationOidcServerDef
## Overview
Definition for OIDC server configuration records.

**Inheritance**
EntityDefinition -> ApplicationOidcServerDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ApplicationOidcServerDef() | Initializes the definition. |

## Fields
| Field | Type | Description |
|:------------|:-------------|:-------------|
| c_application_id | Column&lt;string&gt; | Application identifier. |
| vc_url_wellknown | Column&lt;string?&gt; | Well-known configuration URL. |
| vc_url_jwks | Column&lt;string?&gt; | JWKS endpoint URL. |
| vc_url_authorize | Column&lt;string?&gt; | Authorization endpoint URL. |
| vc_url_token_backchannel | Column&lt;string?&gt; | Back-channel token endpoint URL. |
| vc_url_endsession | Column&lt;string?&gt; | End session endpoint URL. |
| FKApplicationsClients | EntityForeignKey&lt;Applications, ApplicationOidcServer&gt; | Link to parent application. |

## See Also
- [ApplicationOidcServer](../ApplicationOidcServer/index.md)
