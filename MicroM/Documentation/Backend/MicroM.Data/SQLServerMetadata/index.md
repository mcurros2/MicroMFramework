# Class: MicroM.Data.SQLServerMetadata
## Overview
Encapsulates SQL Server type metadata for a column.

**Inheritance**
object -> SQLServerMetadata

**Implements**
None

## Example Usage
```csharp
var meta = new SQLServerMetadata(SqlDbType.Int);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| SQLServerMetadata(SqlDbType sql_type, int size = 0, byte precision = 0, byte scale = 0, bool output = false, bool nullable = false, bool encrypted = false, bool isArray = false) | Initializes the metadata. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| SQLType | SqlDbType | The SQL Server data type. |
| Size | int | The size of the type. |
| Precision | byte | Numeric precision. |
| Scale | byte | Numeric scale. |
| Output | bool | Indicates whether parameter is an output. |
| Nullable | bool | Indicates whether null values are allowed. |
| Encrypted | bool | Indicates whether the value is encrypted. |
| IsArray | bool | Indicates whether the value represents an array. |

## Methods
| Method | Description |
|:------------|:-------------|
| None | |

