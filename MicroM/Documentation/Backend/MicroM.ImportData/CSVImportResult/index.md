# Class: MicroM.ImportData.CSVImportResult
## Overview
Represents the result of importing CSV or Excel data.

**Inheritance**
object -> CSVImportResult

**Implements**
None

## Example Usage
```csharp
var result = new CSVImportResult();
```

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| ProcessedCount | int | Total rows processed. |
| SuccessCount | int | Rows imported successfully. |
| ErrorCount | int | Rows that failed to import. |
| Errors | Dictionary<int, string> | Errors keyed by row number. |

## Methods
| Method | Description |
|:------------|:-------------|
| None |

## Remarks
None.

