# Class: MicroM.Data.DataResultChannel
## Overview
Channel-based buffer for streaming tabular records.

**Inheritance**
object -> DataResultChannel

**Implements**
None

## Example Usage
```csharp
var channel = new DataResultChannel(3);
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| DataResultChannel(int columns, int? buffer_records = null) | Creates a channel for tabular records. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Header | string[] | Column headers for records. |
| Records | Channel<object[]> | Channel containing the records. |

## Methods
| Method | Description |
|:------------|:-------------|
| None | |

## See Also
-
