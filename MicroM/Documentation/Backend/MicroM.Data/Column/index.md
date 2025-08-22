# Class: MicroM.Data.Column<T>
## Overview
Represents a typed database column and factory helpers.

### Type Parameters
| Parameter | Description |
|:------------|:-------------|
|T|Type of the column value.|

**Inheritance**
[ColumnBase](../ColumnBase/index.md) -> Column

**Implements**
-

## Example Usage
```csharp
var col = new Column<int>("Id", 1);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| Column(string name, T value, SqlDbType? sqlType, int size, byte precision, byte scale, bool output, ColumnFlags columnFlags, bool? nullable, bool fake, bool encrypted, bool isArray, string? overrideWith) | Initializes a new instance. |
| Column(Column<T> original, string newName, bool output) | Copies an existing column. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Value | T | Typed column value. |

## Methods
| Method | Description |
|:------------|:-------------|
| PK(...) | Creates a primary key column. |
| FK(...) | Creates a foreign key column. |
| Text(...) | Creates a text column. |
| Char(...) | Creates a fixed-length char column. |
| Clone() | Creates a copy of this column. |
| EmbedCategory(...) | Creates a column embedding a category. |
| EmbedStatus(...) | Creates a column embedding a status. |

