using MicroM.Data;

namespace MicroM.ImportData;

public sealed class ImportDataWebAPIRequest : DataWebAPIRequest
{
    public FileImportMapping? FileImportMapping { get; set; }
    public int? initialRow { get; set; }
}
