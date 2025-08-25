# Interface: MicroM.Data.IGetFieldValue
## Overview
Provides accessors to retrieve field values from a data reader.

## Methods
| Method | Description |
|:------------|:-------------|
| GetFieldValueAsync<T>(int position, CancellationToken ct) | Asynchronously retrieves value by column position. |
| GetFieldValueAsync<T>(string column_name, CancellationToken ct) | Asynchronously retrieves value by column name. |
| GetFieldValue<T>(int position) | Retrieves value by column position. |
| GetFieldValue<T>(string column_name) | Retrieves value by column name. |

## Remarks
None.

