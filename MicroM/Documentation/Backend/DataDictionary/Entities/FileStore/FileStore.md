# FileStore

Metadata for a single stored file.

## Columns
- `c_file_id` (PK)
- `c_fileprocess_id` (FK to `FileStoreProcess`)
- `vc_filename`
- `vc_filefolder`
- `vc_fileguid`
- `bi_filesize`
- `c_fileuploadstatus_id` – status identifier

## Relationships
- `FKFileStoreProcess` – links to `FileStoreProcess`.
- `UCFileStore` unique constraint on `vc_fileguid`.

## Typical Usage
Tracks files on disk and their upload status. Procedure `fst_getByGUID` fetches metadata using the GUID.
