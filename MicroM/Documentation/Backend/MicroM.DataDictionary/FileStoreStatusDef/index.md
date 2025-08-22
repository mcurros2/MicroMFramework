# Class: MicroM.DataDictionary.FileStoreStatusDef
## Overview
Schema definition linking files to their status values.

**Inheritance**
EntityDefinition -> FileStoreStatusDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| FileStoreStatusDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_file_id | Column<string> | Identifier of the file. |
| c_status_id | Column<string> | Identifier of the status set. |
| c_statusvalue_id | Column<string> | Identifier of the status value. |
| fsts_brwStandard | ViewDefinition | Browse view exposing file and status keys. |
| FKFileStoreStatus | EntityForeignKey<FileStore, FileStoreStatus> | Link to the associated file. |
| FKStatus | EntityForeignKey<StatusValues, FileStoreStatus> | Link to the status value. |

## See Also
- [FileStoreStatus](../FileStoreStatus/index.md)
- [FileStore](../FileStore/index.md)
