using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;


public class FileStoreProcessDef : EntityDefinition
{
    public FileStoreProcessDef() : base("fsp", nameof(FileStoreProcess)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_fileprocess_id = Column<string>.PK(autonum: true);

    public readonly ViewDefinition fsp_brwStandard = new(nameof(c_fileprocess_id));

}

public class FileStoreProcess : Entity<FileStoreProcessDef>
{
    public FileStoreProcess() : base() { }
    public FileStoreProcess(string? schema_name) : base(schema_name) { }
    public FileStoreProcess(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }


}
