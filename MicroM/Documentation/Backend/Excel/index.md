# Excel

MicroM provides utilities for working with Excel (`.xlsx`) spreadsheets via the [`MicroM.Excel`](../../..//core/Excel) namespace.

## Reading spreadsheets

`ExcelReader` reads a worksheet stream asynchronously and yields each row as an array of values. The first row is treated as the header and subsequent rows contain typed values based on the cell's data type.

```csharp
await using var stream = File.OpenRead("data.xlsx");

await foreach (var row in ExcelReader.ReadExcelAsync(stream, "Sheet1"))
{
    // row[0], row[1], ...
}
```

The reader inspects each cell and converts numbers, booleans and dates to `double`, `bool` and `DateTime` respectively. If a cell contains a shared string or is empty, the corresponding element remains a `string` or `null`.

## Writing `DataResult` to Excel

`DataResult` represents tabular results in MicroM. The `SaveAsExcelToStreamAsync` extension creates an Excel file from a `DataResult` instance:

```csharp
var data = new DataResult(
    new[] { "Header1", "Header2" },
    new[] { "string", "double" });

data.records.Add(new object?[] { "Value1", 123.45 });

using var stream = new MemoryStream();
await data.SaveAsExcelToStreamAsync(stream, "Sheet1");
```

The extension writes the headers and records to a worksheet, handling null, numeric, boolean and date values appropriately. The resulting workbook can later be parsed using `ExcelReader`.
