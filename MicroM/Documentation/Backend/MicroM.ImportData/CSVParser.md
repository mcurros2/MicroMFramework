# Class: MicroM.ImportData.CSVParser

## Overview
Provides helpers to parse CSV data into dictionaries.

## Methods
| Method | Description |
|:--|:--|
| Parse(string csvData, CancellationToken ct) | Converts CSV text to a list of dictionaries. |
| ParseFile(string file_path, CancellationToken ct) | Reads a CSV file and parses its contents. |

## Remarks
Parsing stops if row length mismatches headers.

## See Also
- [CSVImportResult](CSVImportResult.md)
