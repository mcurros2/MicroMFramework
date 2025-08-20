# Class: MicroM.DataDictionary.StatusDefs.FileUpload

## Overview
Defines status values for tracking file upload operations.

## Fields
| Field | Description |
|:--|:--|
| Pending | Upload has not started. |
| Uploading | File is being uploaded. |
| Uploaded | Upload completed successfully. |
| Failed | Upload encountered an error. |
| Cancelled | Upload was cancelled. |

## Remarks
Extends [StatusDefinition](../MicroM.DataDictionary.Configuration/StatusDefinition.md) with file upload states.
