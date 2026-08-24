using MicroM.Data;

namespace MicroM.ImportData;

public sealed class ImportDataWebAPIRequest : DataWebAPIRequest
{
    public ExcelImportMapping? ExcelImportMapping { get; set; }
}
