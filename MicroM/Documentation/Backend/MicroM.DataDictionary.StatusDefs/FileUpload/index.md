# Class: MicroM.DataDictionary.StatusDefs.FileUpload
## Overview
Represents stages of a file upload operation.

**Inheritance**
StatusDefinition -> FileUpload

## Constructors
| Constructor | Description |
|:------------|:-------------|
| FileUpload() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Pending | StatusValuesDefinition | Upload is queued but not started. |
| Uploading | StatusValuesDefinition | Data is currently being transferred. |
| Uploaded | StatusValuesDefinition | Upload completed successfully. |
| Failed | StatusValuesDefinition | Upload failed due to an error. |
| Cancelled | StatusValuesDefinition | Upload was cancelled before completion. |

## See Also
- [StatusDefinition](../../MicroM.DataDictionary.Configuration/StatusDefinition/index.md)
