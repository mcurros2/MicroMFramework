# Class: MicroM.ImportData.EntityImportData
## Overview
Extension methods for importing entity data from CSV or Excel.

**Inheritance**
object -> EntityImportData

**Implements**
None

## Example Usage
```csharp
await entity.ImportDataFromCSV(rows, options, null, api, "app", null, CancellationToken.None);
```

## Methods
| Method | Description |
|:------------|:-------------|
| MapCSVDataToEntity<T>(T entity, Dictionary<string, string> data) | Maps CSV row values to entity columns. |
| ImportDataFromCSV<T>(T entity, List<Dictionary<string, string>> data, MicroMOptions options, Dictionary<string, object>? claims, IWebAPIServices api, string app_id, Dictionary<string, object>? parentKeys, CancellationToken ct) | Imports parsed CSV data into the entity. |
| ImportDataFromExcel<T>(T entity, Stream excelStream, string? sheetName, int? initialRow, MicroMOptions options, Dictionary<string, object>? claims, IWebAPIServices api, string app_id, Dictionary<string, object>? parentKeys, CancellationToken ct) | Imports data from an Excel stream into the entity. |

## Remarks
None.

## See Also
-
