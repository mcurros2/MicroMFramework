# Class: MicroM.DataDictionary.FileStoreDef
## Overview
Schema definition for persisted files.

**Inheritance**
EntityDefinition -> FileStoreDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| FileStoreDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_file_id | Column<string> | Primary identifier of the stored file. |
| c_fileprocess_id | Column<string> | Identifier of the generating process. |
| vc_filename | Column<string> | File name. |
| vc_filefolder | Column<string> | Folder used for storage. |
| vc_fileguid | Column<string> | Unique file GUID. |
| bi_filesize | Column<long> | File size in bytes. |
| c_fileuploadstatus_id | Column<string> | Status identifier tracking upload progress. |
| fst_brwStandard | ViewDefinition | Default browse view by file ID. |
| fst_brwFiles | ViewDefinition | Browse view for retrieving files by process. |
| fst_getByGUID | ProcedureDefinition | Retrieves a file by its GUID. |
| FKFileStoreProcess | EntityForeignKey<FileStoreProcess, FileStore> | Link to the creating process. |
| UCFileStore | EntityUniqueConstraint | Enforces unique GUID values. |

## See Also
- [FileStore](../FileStore/index.md)
- [FileStoreProcess](../FileStoreProcess/index.md)
