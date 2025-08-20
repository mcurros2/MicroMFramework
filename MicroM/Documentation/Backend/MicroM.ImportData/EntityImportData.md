# Class: MicroM.ImportData.EntityImportData

## Overview
Extension methods to map CSV or Excel rows into entity instances and insert them.

## Methods
| Method | Description |
|:--|:--|
| MapCSVDataToEntity<T>(this T entity, Dictionary<string,string> data) | Maps CSV field values onto entity columns. |
| ImportDataFromCSV<T>(this T entity, List<Dictionary<string,string>> data, MicroMOptions options, Dictionary<string,object>? claims, IWebAPIServices api, string app_id, Dictionary<string,object>? parentKeys, CancellationToken ct) | Imports rows provided as dictionaries using entity's insert logic. |
| ImportDataFromExcel<T>(this T entity, Stream excelStream, string? sheetName, int? initialRow, MicroMOptions options, Dictionary<string,object>? claims, IWebAPIServices api, string app_id, Dictionary<string,object>? parentKeys, CancellationToken ct) | Reads Excel stream and imports rows using entity insert logic. |

## Remarks
Requires entities to derive from EntityBase.

## See Also
- [CSVParser](CSVParser.md)
