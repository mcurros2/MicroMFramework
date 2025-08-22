# Class: MicroM.Data.ColumnBase
## Overview
Base metadata and value handling for database columns.

**Inheritance**
object -> ColumnBase

**Implements**
-

## Example Usage
```csharp
// ColumnBase is abstract; derive a concrete column to use.
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ColumnBase(Type systemType, string name, object? value, SqlDbType? sqlType, int size, byte precision, byte scale, bool output, ColumnFlags columnFlags, bool? nullable, string? relatedCategoryId, bool encrypted, bool isArray, string? overrideWith) | Initializes a new instance. |
| ColumnBase(ColumnBase col, string newName, bool output) | Copies an existing column. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| SystemType | Type | CLR type of the value. |
| ColumnMetadata | ColumnFlags | Flags describing column behavior. |
| SQLMetadata | SQLServerMetadata | SQL Server metadata. |
| Name | string | Column name. |
| ValueObject | object? | Raw column value. |
| SQLParameterName | string | SQL parameter name. |
| RelatedCategoryID | string? | Related category identifier. |
| RelatedStatusID | string? | Related status identifier. |
| OverrideWith | string? | Server claim key used to override value. |

## Methods
| Method | Description |
|:------------|:-------------|
| ToString() | Returns the column name. |

