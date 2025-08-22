
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class FileStoreProcessDef : EntityDefinition
    {
        public FileStoreProcessDef() : base("fsp", nameof(FileStoreProcess)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        public readonly Column<string> c_fileprocess_id = Column<string>.PK(autonum: true);

        public ViewDefinition fsp_brwStandard { get; private set; } = new(nameof(c_fileprocess_id));

    }

    public class FileStoreProcess : Entity<FileStoreProcessDef>
    {
        public FileStoreProcess() : base() { }
        public FileStoreProcess(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }


    }


}
