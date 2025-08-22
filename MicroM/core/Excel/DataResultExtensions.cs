using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MicroM.Data;

namespace MicroM.Excel;

/// <summary>
/// Provides extension methods for exporting <see cref="DataResult"/> instances to Excel.
/// </summary>
public static class DataResultExcelExtensions
{
    /// <summary>
    /// Writes the <see cref="DataResult"/> contents to the specified stream in Excel format.
    /// </summary>
    /// <param name="data">The source data to export.</param>
    /// <param name="outputStream">The target stream where the Excel document will be written.</param>
    /// <param name="sheetName">The name of the worksheet to create.</param>
    public static async Task SaveAsExcelToStreamAsync(this DataResult data, Stream outputStream, string sheetName)
    {
        using var document = SpreadsheetDocument.Create(outputStream, SpreadsheetDocumentType.Workbook, true);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        // Create Shared String Table
        var sharedStringTablePart = workbookPart.AddNewPart<SharedStringTablePart>();
        sharedStringTablePart.SharedStringTable = new SharedStringTable();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        workbookPart.Workbook.AppendChild(new Sheets());
        workbookPart.AddPart(worksheetPart);

        using (var writer = OpenXmlWriter.Create(worksheetPart))
        {
            writer.WriteStartElement(new Worksheet());
            writer.WriteStartElement(new SheetData());

            uint rowIndex = 1;

            writer.WriteStartElement(new Row() { RowIndex = rowIndex });
            foreach (var header in data.Header)
            {
                WriteSharedStringCell(writer, header, sharedStringTablePart);
            }
            writer.WriteEndElement(); // </Row>

            foreach (var record in data.records)
            {
                writer.WriteStartElement(new Row() { RowIndex = ++rowIndex });
                foreach (var cell in record)
                {
                    WriteCell(writer, cell, sharedStringTablePart);
                }
                writer.WriteEndElement(); // </Row>
            }

            writer.WriteEndElement(); // </SheetData>
            writer.WriteEndElement(); // </Worksheet>
        }

        // Save shared string table
        sharedStringTablePart.SharedStringTable.Save();

        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>();

        sheets?.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = sheetName
        });

        workbookPart.Workbook.Save();
        await outputStream.FlushAsync();
    }

    private static void WriteSharedStringCell(OpenXmlWriter writer, string value, SharedStringTablePart sharedStringTablePart)
    {
        int index = InsertSharedStringItem(value, sharedStringTablePart);
        var cell = new Cell
        {
            DataType = CellValues.SharedString,
            CellValue = new CellValue(index.ToString())
        };
        writer.WriteElement(cell);
    }

    private static void WriteCell(OpenXmlWriter writer, object? value, SharedStringTablePart sharedStringTablePart)
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
                    WriteSharedStringCell(writer, text, sharedStringTablePart);
                }
                break;
        }
    }

    private static int InsertSharedStringItem(string text, SharedStringTablePart sharedStringTablePart)
    {
        var items = sharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ToList();
        int index = items.FindIndex(item => item.InnerText == text);

        if (index >= 0)
            return index;

        sharedStringTablePart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
        sharedStringTablePart.SharedStringTable.Save();

        return items.Count;
    }


}
