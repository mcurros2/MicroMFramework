# Class: MicroM.DataDictionary.ApplicationsUrlsDef
## Overview
Schema definition for storing application URLs.

**Inheritance**
EntityDefinition -> ApplicationsUrlsDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ApplicationsUrlsDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_application_id | Column<string> | Identifier of the application. |
| c_application_url_id | Column<string> | Unique identifier for the application URL. |
| vc_application_url | Column<string> | URL value. |
| apu_brwStandard | ViewDefinition | Default browse view for application URLs. |
| FKApplicationsUrls | EntityForeignKey<Applications, ApplicationsUrls> | Relationship to application entity. |
| UNApplicationUrl | EntityUniqueConstraint | Ensures uniqueness of URLs per application. |

## See Also
- [ApplicationsUrls](../ApplicationsUrls/index.md)
