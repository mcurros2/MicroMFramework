using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MicroM.Data;
using Microsoft.AspNetCore.Mvc;
using static System.ArgumentNullException;

namespace MicroM.Excel;

public static class ExcelWriter
{
    public static void WriteSharedStringCell(OpenXmlWriter writer, string value, SharedStringTablePart sharedStringTablePart)
    {
        int index = InsertSharedStringItem(value, sharedStringTablePart);
        var cell = new Cell
        {
            DataType = CellValues.SharedString,
            CellValue = new CellValue(index.ToString())
        };
        writer.WriteElement(cell);
    }

    public static void WriteCell(OpenXmlWriter writer, object? value, SharedStringTablePart sharedStringTablePart)
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

                if (string.IsNullOrEmpty(text))
                {
                    // Empty or null string: write empty cell directly
                    writer.WriteElement(new Cell());
                }
                else
                {
                    // Non-empty string: add to shared strings
                    WriteSharedStringCell(writer, text, sharedStringTablePart);
                }
                break;
        }
    }

    public static int InsertSharedStringItem(string text, SharedStringTablePart sharedStringTablePart)
    {
        var items = sharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>().ToList();
        if (items != null)
        {
            int index = items.FindIndex(item => item.InnerText == text);

            if (index >= 0) return index;

            sharedStringTablePart?.SharedStringTable?.AppendChild(new SharedStringItem(new Text(text)));
            sharedStringTablePart?.SharedStringTable?.Save();

        }

        return items?.Count ?? 0;
    }

    public static async Task WriteSheetAsync(uint sheetId, string sheetName, WorkbookPart workbookPart, SharedStringTablePart sharedStringTablePart, DataResultChannel resultSet, CancellationToken ct)
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
            WriteSharedStringCell(writer, header ?? string.Empty, sharedStringTablePart);
        }
        writer.WriteEndElement(); // </Row>

        await foreach (var record in resultSet.records.Reader.ReadAllAsync(ct))
        {
            writer.WriteStartElement(new Row { RowIndex = ++rowIndex });
            foreach (var cell in record)
            {
                WriteCell(writer, cell, sharedStringTablePart);
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
        var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sharedStringTablePart = workbookPart.AddNewPart<SharedStringTablePart>();
            sharedStringTablePart.SharedStringTable = new SharedStringTable();

            uint sheetId = 1;

            await foreach (var resultSet in resultChannel.Results.Reader.ReadAllAsync(ct))
            {
                var sheetName = sheetId == 1 ? baseSheetName : $"{baseSheetName}_{sheetId}";
                await WriteSheetAsync(sheetId, sheetName, workbookPart, sharedStringTablePart, resultSet, ct);
                sheetId++;
            }

            // Propagate producer errors
            await producerTask;

            sharedStringTablePart.SharedStringTable.Save();
            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        var fileName = $"{baseSheetName}_{DateTime.Now}.xlsx";
        return new FileStreamResult(stream, ExcelContentType)
        {
            FileDownloadName = fileName
        };
    }

}
