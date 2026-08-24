using Sylvan.Data.Excel;
using System.Runtime.CompilerServices;

namespace MicroM.Excel;

public static class ExcelReader
{
    public static IAsyncEnumerable<object?[]> ReadExcelAsync(
        Stream stream,
        string? sheetName,
        int initialRow = 1,
        CancellationToken ct = default)
    {
        return ReadExcelAsync(stream, ExcelWorkbookType.ExcelXml, sheetName, initialRow, ct);
    }

    public static async IAsyncEnumerable<object?[]> ReadExcelAsync(
        Stream stream,
        ExcelWorkbookType workbookType,
        string? sheetName,
        int initialRow = 1,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (initialRow < 1) throw new ArgumentOutOfRangeException(nameof(initialRow), "The initial row must be greater than zero.");
        if (workbookType == ExcelWorkbookType.Unknown) throw new ArgumentException("The Excel workbook type must be specified.", nameof(workbookType));

        ExcelDataReaderOptions readerOptions = new()
        {
            OwnsStream = false,
            Schema = ExcelSchema.DynamicNoHeaders
        };

        await using var reader = await ExcelDataReader.CreateAsync(stream, workbookType, readerOptions, ct);

        if (!string.IsNullOrEmpty(sheetName) && !reader.TryOpenWorksheet(sheetName))
        {
            throw new InvalidDataException($"The worksheet '{sheetName}' was not found.");
        }

        int columnCount = -1;
        while (await reader.ReadAsync(ct))
        {
            if (reader.RowNumber < initialRow) continue;

            if (columnCount < 0) columnCount = reader.RowFieldCount;

            object?[] row = new object?[columnCount];
            int valuesToRead = Math.Min(columnCount, reader.RowFieldCount);
            for (int i = 0; i < valuesToRead; i++)
            {
                if (reader.RowNumber == initialRow)
                {
                    row[i] = reader.GetExcelDataType(i) == ExcelDataType.Null ? null : reader.GetString(i);
                }
                else
                {
                    var value = reader.GetValue(i);
                    row[i] = value == DBNull.Value
                        ? reader.GetExcelDataType(i) == ExcelDataType.String ? reader.GetString(i) : null
                        : value;
                }
            }

            yield return row;
        }
    }
}
