# Import Data

This section describes the backend utilities for importing data into MicroM.

## CSVParser

- A static helper that converts CSV text into `List<Dictionary<string,string>>`.
- `Parse` splits the first line as headers, unquotes values, and creates a dictionary for each row.
- `ParseFile` asynchronously reads a file and delegates to `Parse`.

## CSVImportResult

Summarizes bulk import operations:

- `ProcessedCount`
- `SuccessCount`
- `ErrorCount`
- `Errors`: dictionary mapping row number to error message.

## EntityImportData

Extension methods for `EntityBase` that map data and insert it.

### MapCSVDataToEntity

- Accepts a dictionary of column names and string values.
- Converts data into SQL column types (strings, booleans, numbers, decimals, dates, GUIDs, binary).

### ImportDataFromCSV

- Iterates over parsed CSV rows.
- Applies application and parent keys.
- Inserts each row and collects results in `CSVImportResult`.

### ImportDataFromExcel

- Reads workbook rows with `ExcelReader.ReadExcelAsync`.
- Uses the first row as headers, converts subsequent rows to a dictionary, then follows the same import pipeline.

## Excel Helpers Integration

`EntityImportData.ImportDataFromExcel` leverages `MicroM.Excel.ExcelReader` to stream typed rows from Excel files, enabling the same import logic used for CSV to handle spreadsheets seamlessly.

