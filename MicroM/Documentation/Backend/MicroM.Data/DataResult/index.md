# Class: MicroM.Data.DataResult
## Overview
Represents tabular data results.

**Inheritance**
object -> DataResult

**Implements**
None

## Example Usage
```csharp
var result = new MicroM.Data.DataResult(2);
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| DataResult(int columns) | Initializes with specified number of columns. |
| DataResult(string[] headers, string[] type_info) | Initializes with headers and type information. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Header | string[] | Column names for the result set. |
| typeInfo | string[] | Type information for each column. |
| records | List<object?[]> | Record values. |

## Methods
| Method | Description |
|:------------|:-------------|
| this[int record, string key] | Retrieves a value for the given record and column. |

## Remarks
None.

