# Class: MicroM.DataDictionary.StatusDefs.EmailStatus

## Overview
Represents statuses for outgoing emails handled by the system.

## Fields
| Field | Description |
|:--|:--|
| QUEUED | Email queued for processing. |
| PROCESSING | Email is being processed. |
| SENT | Email sent successfully. |
| ERROR | An error occurred while sending. |
| RETRY | Pending retry after failure. |

## Remarks
Derived from [StatusDefinition](../MicroM.DataDictionary.Configuration/StatusDefinition.md) to model email workflow states.
