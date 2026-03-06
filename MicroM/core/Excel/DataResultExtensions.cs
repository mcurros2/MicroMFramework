using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MicroM.Configuration;
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

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        workbookPart.Workbook.AppendChild(new Sheets());
        workbookPart.AddPart(worksheetPart);

        var sharedStringTableCache = new Dictionary<string, int>(DataDefaults.DefaultExportToExcelSharedStringDictionaryCapacity, StringComparer.Ordinal);
        using (var writer = OpenXmlWriter.Create(worksheetPart))
        {

            writer.WriteStartElement(new Worksheet());
            writer.WriteStartElement(new SheetData());

            uint rowIndex = 1;

            writer.WriteStartElement(new Row() { RowIndex = rowIndex });
            foreach (var header in data.Header)
            {
                WriteSharedStringCell(writer, header, sharedStringTableCache);
            }
            writer.WriteEndElement(); // </Row>

            foreach (var record in data.records)
            {
                writer.WriteStartElement(new Row() { RowIndex = ++rowIndex });
                foreach (var cell in record)
                {
                    WriteCell(writer, cell, sharedStringTableCache);
                }
                writer.WriteEndElement(); // </Row>
            }



            writer.WriteEndElement(); // </SheetData>
            writer.WriteEndElement(); // </Worksheet>
        }

        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>();

        sheets?.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = sheetName
        });

        WriteSharedStringTablePart(workbookPart, sharedStringTableCache);

        workbookPart.Workbook.Save();
        await outputStream.FlushAsync();
    }


}
