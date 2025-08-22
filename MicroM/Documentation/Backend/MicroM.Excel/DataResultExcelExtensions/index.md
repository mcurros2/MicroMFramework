# Class: MicroM.Excel.DataResultExcelExtensions
## Overview
Provides an extension method to save a DataResult as an Excel worksheet.

**Inheritance**
object -> DataResultExcelExtensions

**Implements**
None

## Example Usage
```csharp
await dataResult.SaveAsExcelToStreamAsync(stream, "Sheet1");
```
## Methods
| Method | Description |
|:------------|:-------------|
| SaveAsExcelToStreamAsync(DataResult data, Stream outputStream, string sheetName) | Writes the DataResult to the provided stream in Excel format. |

## Remarks
None.

## See Also
- [ExcelReader](../ExcelReader/index.md)
