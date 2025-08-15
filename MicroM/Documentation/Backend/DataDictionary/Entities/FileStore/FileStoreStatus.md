# FileStoreStatus

Associates status values with stored files.

## Columns
- `c_file_id` (PK, FK to `FileStore`)
- `c_status_id` (PK)
- `c_statusvalue_id` (FK to `StatusValues`)

## Relationships
- `FKFileStoreStatus` – links to `FileStore`.
- `FKStatus` – links to `StatusValues`.

## Typical Usage
Used to track the lifecycle of a file (uploaded, processed, etc.).
