# Class: MicroM.ImportData.CSVImportResult

## Overview
Represents the result of a CSV import operation.

## Properties
| Property | Type | Description |
|:--|:--|:--|
| ProcessedCount | int | Total rows processed. |
| SuccessCount | int | Rows imported successfully. |
| ErrorCount | int | Rows that failed to import. |
| Errors | Dictionary<int, string> | Line-specific error messages. |

## Remarks
Used to report progress and errors during CSV or Excel import.

## See Also
- [CSVParser](CSVParser.md)
