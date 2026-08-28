namespace MicroM.ImportData;

public sealed record FileImportColumnMapping(string? SourceHeader, int? SourceIndex, string DestinationColumnName);

public sealed class FileImportMapping
{
    public string? SheetName { get; init; }
    public FileImportColumnMapping[] Mapping { get; init; } = [];
}
