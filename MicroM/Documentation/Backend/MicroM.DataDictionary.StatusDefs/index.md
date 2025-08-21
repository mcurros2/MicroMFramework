# Namespace: MicroM.DataDictionary.StatusDefs
## Overview
Provides status definitions for core operations such as emails, file uploads, data imports, and generic processes.

## Classes
| Class | Description |
|:------------|:-------------|
| [EmailStatus](EmailStatus/index.md) | Status values for the email sending pipeline. |
| [FileUpload](FileUpload/index.md) | Tracks stages of a file upload. |
| [ImportStatus](ImportStatus/index.md) | Represents states of data import operations. |
| [ProcessStatus](ProcessStatus/index.md) | Basic start and completion flags for a process. |

## Enums
| Enum | Description |
|:------------|:-------------|

## Structs
| Struct | Description |
|:------------|:-------------|

## Interfaces
| Interface | Description |
|:------------|:-------------|

## Remarks
Each status definition derives from `StatusDefinition` and exposes named `StatusValuesDefinition` fields indicating possible states.

## See Also
- [MicroM.DataDictionary](../MicroM.DataDictionary/index.md)
- [MicroM.DataDictionary.Configuration](../MicroM.DataDictionary.Configuration/index.md)
