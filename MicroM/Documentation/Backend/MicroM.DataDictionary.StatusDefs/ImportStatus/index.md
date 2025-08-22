# Class: MicroM.DataDictionary.StatusDefs.ImportStatus
## Overview
Describes the progression of a data import task.

**Inheritance**
StatusDefinition -> ImportStatus

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ImportStatus() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Pending | StatusValuesDefinition | Import has been queued but not started. |
| FormatError | StatusValuesDefinition | The file format is invalid. |
| Importing | StatusValuesDefinition | Import is currently running. |
| Completed | StatusValuesDefinition | Import completed successfully. |
| Error | StatusValuesDefinition | An unexpected error occurred during import. |

## See Also
- [StatusDefinition](../../MicroM.DataDictionary.Configuration/StatusDefinition/index.md)
