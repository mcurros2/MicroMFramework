# Class: MicroM.Extensions.DataExtensions
## Overview
Convenience methods for working with `DataResult` objects and SQL utilities.

**Inheritance**
object -> DataExtensions

**Implements**
None

## Example Usage
```csharp
if (results.HasData()) {
    var names = results.ToListOfStringColumn(0);
}
```
## Methods
| Method | Description |
|:------------|:-------------|
| HasData(List<DataResult>? result) | Determines if the result set contains data. |
| ToDictionaryOfStringRecord(DataResult result, int record_index, StringComparer? comparer) | Converts a record to a dictionary of strings. |
| ToListOfStringColumn(DataResult result, int header_index) | Returns a list of values from a column. |
| ToDictionary(DataResult result, int record_index) | Converts a record to a dictionary of objects. |
| GetHeaderIndex(DataResult result, string column_name) | Gets the index of a column header. |
| Get<TColumn>(DataResult result, string column_name, int record) | Retrieves a typed column value. |
| TraceSQL(SqlCommand cmd) | Produces a traceable SQL string. |
| FromJsonStringArray(string? json_string_array, bool dont_throw_exception = true) | Parses a JSON string array into a string array. |

## Remarks
None.

