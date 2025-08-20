# Class: MicroM.DataDictionary.StatusDefs.ImportStatus

## Overview
Tracks status values for data import operations.

## Fields
| Field | Description |
|:--|:--|
| Pending | Import not yet started. |
| FormatError | File format is invalid. |
| Importing | Data is currently being imported. |
| Completed | Import finished successfully. |
| Error | Import failed. |

## Remarks
Extends [StatusDefinition](../MicroM.DataDictionary.Configuration/StatusDefinition.md) with import-specific states.
