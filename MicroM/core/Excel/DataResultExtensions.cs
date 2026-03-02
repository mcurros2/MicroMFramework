using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MicroM.Data;
using static MicroM.Excel.ExcelWriter;

namespace MicroM.Excel;

public static class DataResultExcelExtensions
{
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


}
