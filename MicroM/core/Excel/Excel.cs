using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MicroM.Excel;

/// <summary>
/// Provides methods for reading data from Excel spreadsheets.
/// </summary>
public static class ExcelReader
{
    /// <summary>
    /// Asynchronously reads rows from an Excel stream, starting after the header row.
    /// </summary>
    /// <param name="stream">The Excel file stream to read from.</param>
    /// <param name="sheetName">The worksheet name to read, or <c>null</c> for the first sheet.</param>
    /// <param name="initialRow">The 1-based header row index.</param>
    /// <returns>An asynchronous sequence of row values.</returns>
    public static async IAsyncEnumerable<object?[]> ReadExcelAsync(Stream stream, string? sheetName, int initialRow = 1)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart;

        if (workbookPart == null)
            yield break;

        var sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>()
            .FirstOrDefault(s => (s.Name?.Value == sheetName) || sheetName == null);

        if (sheet == null || sheet.Id == null)
            yield break;

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        var rows = worksheetPart.Worksheet.Descendants<Row>();

        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

        var headerRow = rows.FirstOrDefault(r => r.RowIndex?.Value == initialRow);
        if (headerRow == null)
            yield break;

        var headers = headerRow.Elements<Cell>()
            .Select(c => GetCellValue(c, sharedStringTable))
            .ToArray();

        yield return headers;
        await Task.Yield();

        foreach (var row in rows.Where(r => r.RowIndex?.Value > initialRow))
        {

            var cellElements = row.Elements<Cell>().ToArray();
            var rowData = new object?[headers.Length];

            for (int i = 0; i < headers.Length; i++)
            {
                if (i < cellElements.Length)
                    rowData[i] = GetTypedCellValue(cellElements[i], sharedStringTable);
                else
                    rowData[i] = null;
            }

            yield return rowData;
            await Task.Yield();
        }
    }

    private static object? GetTypedCellValue(Cell cell, SharedStringTable? sst)
    {
        var value = cell.CellValue?.Text;
        if (value == null)
            return null;

        if (cell.DataType == null)
            return double.TryParse(value, out double numericValue) ? numericValue : value;

        if (cell.DataType == CellValues.SharedString)
            return int.TryParse(value, out int index) && sst != null && index >= 0 && index < sst.Count()
                ? sst.ElementAt(index).InnerText
                : value;

        if (cell.DataType == CellValues.Boolean)
            return value == "1";

        if (cell.DataType == CellValues.Number)
            return double.TryParse(value, out double numericValue) ? numericValue : value;

        if (cell.DataType == CellValues.Date)
            return DateTime.TryParse(value, out DateTime dateValue) ? dateValue : value;

        // Default to string
        return value;
    }

    private static string? GetCellValue(Cell cell, SharedStringTable? sst)
    {
        var value = cell.CellValue?.Text;
        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
        {
            if (int.TryParse(value, out int index) && sst != null && index >= 0 && index < sst.Count())
                return sst.ElementAt(index).InnerText;
            return null;
        }

        return value;
    }

}
