# Class: MicroM.ImportData.CSVParser
## Overview
Helpers for parsing CSV data into row dictionaries.

**Inheritance**
object -> CSVParser

**Implements**
None

## Example Usage
```csharp
var rows = CSVParser.Parse(csv, CancellationToken.None);
```

## Methods
| Method | Description |
|:------------|:-------------|
| Parse(string csvData, CancellationToken ct) | Parses CSV text into a list of dictionaries. |
| ParseFile(string file_path, CancellationToken ct) | Reads and parses a CSV file. |

## Remarks
None.

