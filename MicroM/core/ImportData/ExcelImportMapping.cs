namespace MicroM.ImportData;

public sealed record ImportDataMapping(string SourceHeader, int SourceIndex, string DestinationColumnName);

public sealed class ExcelImportMapping
{
    public string? SheetName { get; init; }
    public ImportDataMapping[] Mapping { get; init; } = [];
}
