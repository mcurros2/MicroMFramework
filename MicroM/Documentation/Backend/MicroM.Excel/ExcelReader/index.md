# Class: MicroM.Excel.ExcelReader
## Overview
Provides utilities to read Excel spreadsheets asynchronously.

**Inheritance**
object -> ExcelReader

**Implements**
None

## Example Usage
```csharp
await foreach (var row in ExcelReader.ReadExcelAsync(stream, null))
{
    // Process row data
}
```
## Methods
| Method | Description |
|:------------|:-------------|
| ReadExcelAsync(Stream stream, string? sheetName, int initialRow = 1) | Reads an Excel stream and yields rows starting from the specified row. |

## Remarks
None.

## See Also
- [DataResultExcelExtensions](../DataResultExcelExtensions/index.md)
