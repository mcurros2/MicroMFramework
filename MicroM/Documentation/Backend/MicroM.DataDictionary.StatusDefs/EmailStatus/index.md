# Class: MicroM.DataDictionary.StatusDefs.EmailStatus
## Overview
Defines possible states for emails processed by the system.

**Inheritance**
StatusDefinition -> EmailStatus

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EmailStatus() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| QUEUED | StatusValuesDefinition | Email is queued but not yet processed. |
| PROCESSING | StatusValuesDefinition | Email is currently being processed. |
| SENT | StatusValuesDefinition | Email was sent successfully. |
| ERROR | StatusValuesDefinition | An error occurred while sending. |
| RETRY | StatusValuesDefinition | Email failed and is pending retry. |

## See Also
- [StatusDefinition](../../MicroM.DataDictionary.Configuration/StatusDefinition/index.md)
