using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MicroM.Configuration;
using MicroM.Data;
using Microsoft.AspNetCore.Mvc;
using static System.ArgumentNullException;

namespace MicroM.Excel;

public static class ExcelWriter
{
    public static void WriteStringCell(OpenXmlWriter writer, string text)
    {
        writer.WriteStartElement(new Cell { DataType = CellValues.InlineString });
        writer.WriteElement(new InlineString(new Text(text)));
        writer.WriteEndElement();
    }

    public static void WriteSharedStringCell(OpenXmlWriter writer, string value, SharedStringTablePart sharedStringTablePart, Dictionary<string, int> sharedStringCache)
    {
        int index = InsertSharedStringItem(value, sharedStringTablePart, sharedStringCache);
        var cell = new Cell
        {
            DataType = CellValues.SharedString,
            CellValue = new CellValue(index.ToString())
        };
        writer.WriteElement(cell);
    }

    public static void WriteCell(OpenXmlWriter writer, object? value, SharedStringTablePart sharedStringTablePart, Dictionary<string, int> sharedStringCache)
    {
        if (value == null)
        {
            writer.WriteElement(new Cell());
            return;
        }

        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Double:
            case TypeCode.Single:
            case TypeCode.Decimal:
                writer.WriteElement(new Cell
                {
                    DataType = CellValues.Number,
                    CellValue = new CellValue(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "")
                });
                break;

            case TypeCode.Boolean:
                writer.WriteElement(new Cell
                {
                    DataType = CellValues.Boolean,
                    CellValue = new CellValue((bool)value ? "1" : "0")
                });
                break;

            case TypeCode.DateTime:
                writer.WriteElement(new Cell
                {
                    DataType = CellValues.Date,
                    CellValue = new CellValue(((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss"))
                });
                break;

            default:
                var text = value?.ToString();

                if (text == null)
                {
                    // Empty or null string: write empty cell directly
                    writer.WriteElement(new Cell());
                }
                else
                {
                    // Non-empty string: add to shared strings
                    WriteSharedStringCell(writer, text, sharedStringTablePart, sharedStringCache);
                }
                break;
        }
    }

    public static int InsertSharedStringItem(string text, SharedStringTablePart sharedStringTablePart, Dictionary<string, int> sharedStringCache)
    {
        if (sharedStringCache.TryGetValue(text, out int index))
        {
            return index;
        }
        var sharedStringTable = sharedStringTablePart.SharedStringTable;
        sharedStringTable!.AppendChild(new SharedStringItem(new Text(text)));

        index = sharedStringCache.Count;
        sharedStringCache[text] = index;

        return index;
    }

    public static async Task WriteSheetAsync(uint sheetId, string sheetName, WorkbookPart workbookPart, SharedStringTablePart sharedStringTablePart, DataResultChannel resultSet, Dictionary<string, int> sharedStringCache, CancellationToken ct)
    {
        ThrowIfNull(workbookPart.Workbook, nameof(workbookPart.Workbook));

        var sheetPart = workbookPart.AddNewPart<WorksheetPart>();
        using var writer = OpenXmlWriter.Create(sheetPart);

        writer.WriteStartElement(new Worksheet());
        writer.WriteStartElement(new SheetData());

        uint rowIndex = 1;

        writer.WriteStartElement(new Row { RowIndex = rowIndex });
        foreach (var header in resultSet.Header)
        {
            WriteSharedStringCell(writer, header ?? string.Empty, sharedStringTablePart, sharedStringCache);
        }
        writer.WriteEndElement(); // </Row>

        await foreach (var record in resultSet.records.Reader.ReadAllAsync(ct))
        {
            writer.WriteStartElement(new Row { RowIndex = ++rowIndex });
            foreach (var cell in record)
            {
                WriteCell(writer, cell, sharedStringTablePart, sharedStringCache);
            }
            writer.WriteEndElement(); // </Row>
        }

        writer.WriteEndElement(); // </SheetData>
        writer.WriteEndElement(); // </Worksheet>

        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(sheetPart),
            SheetId = sheetId,
            Name = sheetName
        });
    }

    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static async Task<FileStreamResult> ExportExcelFromChannelAsync(string baseSheetName, DataResultSetChannel resultChannel, Task producerTask, CancellationToken ct)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");

        using var writeStream = new FileStream(
                   tempPath,
                   FileMode.CreateNew,
                   FileAccess.ReadWrite,
                   FileShare.None,
                   bufferSize: DataDefaults.DefaultExportToExcelFileStreamCapacity,
                   options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var document = SpreadsheetDocument.Create(writeStream, SpreadsheetDocumentType.Workbook, false);

        var sharedStringTableCache = new Dictionary<string, int>(DataDefaults.DefaultExportToExcelSharedStringDictionaryCapacity, StringComparer.Ordinal);

        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var sharedStringTablePart = workbookPart.AddNewPart<SharedStringTablePart>();
        sharedStringTablePart.SharedStringTable = new SharedStringTable();

        uint sheetId = 1;

        await foreach (var resultSet in resultChannel.Results.Reader.ReadAllAsync(ct))
        {
            var sheetName = sheetId == 1 ? baseSheetName : $"{baseSheetName}_{sheetId}";
            await WriteSheetAsync(sheetId, sheetName, workbookPart, sharedStringTablePart, resultSet, sharedStringTableCache, ct);
            sheetId++;
        }

        // Propagate producer errors
        await producerTask;

        sharedStringTablePart.SharedStringTable.Save();
        workbookPart.Workbook.Save();

        sharedStringTableCache.Clear();

        // Reopen the temp file for read; DeleteOnClose ensures cleanup when the response ends.
        var readStream = new FileStream(
            tempPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: DataDefaults.DefaultExportToExcelFileStreamCapacity,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

        var fileName = $"{baseSheetName}_{DateTime.Now}.xlsx";
        return new FileStreamResult(readStream, ExcelContentType)
        {
            FileDownloadName = fileName
        };
    }

}
