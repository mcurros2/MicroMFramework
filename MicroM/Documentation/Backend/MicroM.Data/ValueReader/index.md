# Class: MicroM.Data.ValueReader
## Overview
Wraps a SqlDataReader and exposes value retrieval helpers.

**Inheritance**
object -> ValueReader

**Implements**
[IGetFieldValue](../IGetFieldValue/index.md)

## Example Usage
```csharp
var vr = new ValueReader(reader);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ValueReader(SqlDataReader reader) | Creates the wrapper. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| - | - | - |

## Methods
| Method | Description |
|:------------|:-------------|
| GetFieldValue<T>(int position) | See DbDataReader.GetFieldValue. |
| GetFieldValue<T>(string column_name) | Access by column name. |
| GetFieldValueAsync<T>(int position, CancellationToken ct) | Async by position. |
| GetFieldValueAsync<T>(string column_name, CancellationToken ct) | Async by name. |

## See Also
- [IGetFieldValue](../IGetFieldValue/index.md)
