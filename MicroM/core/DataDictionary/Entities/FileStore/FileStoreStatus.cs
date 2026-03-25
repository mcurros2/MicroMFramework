
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;


public class FileStoreStatusDef : EntityDefinition
{
    public FileStoreStatusDef() : base("fsts", nameof(FileStoreStatus)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_file_id = Column<string>.PK();
    public readonly Column<string> c_status_id = Column<string>.PK();
    public readonly Column<string> c_statusvalue_id = Column<string>.FK();

    public readonly ViewDefinition fsts_brwStandard = new(nameof(c_file_id), nameof(c_status_id));

    public readonly EntityForeignKey<FileStore, FileStoreStatus> FKFileStoreStatus = new();
    public readonly EntityForeignKey<StatusValues, FileStoreStatus> FKStatus = new();

}

public class FileStoreStatus : Entity<FileStoreStatusDef>
{
    public FileStoreStatus() : base() { }
    public FileStoreStatus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


}
