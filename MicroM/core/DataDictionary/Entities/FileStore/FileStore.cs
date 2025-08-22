
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.StatusDefs;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class FileStoreDef : EntityDefinition
    {
        public FileStoreDef() : base("fst", nameof(FileStore)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        public readonly Column<string> c_file_id = Column<string>.PK(autonum: true);
        public readonly Column<string> c_fileprocess_id = Column<string>.FK();
        public readonly Column<string> vc_filename = Column<string>.Text();
        public readonly Column<string> vc_filefolder = Column<string>.Char(size: 6);
        public readonly Column<string> vc_fileguid = Column<string>.Text(column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.FK);
        public readonly Column<long> bi_filesize = new();
        public readonly Column<string> c_fileuploadstatus_id = Column<string>.EmbedStatus(nameof(FileUpload));

        public readonly ViewDefinition fst_brwStandard = new(nameof(c_file_id));
        public readonly ViewDefinition fst_brwFiles = new(nameof(c_file_id), nameof(c_fileprocess_id));

        public readonly ProcedureDefinition fst_getByGUID = new(new[] { nameof(vc_fileguid) });

        public readonly EntityForeignKey<FileStoreProcess, FileStore> FKFileStoreProcess = new();
        public readonly EntityUniqueConstraint UCFileStore = new(keys: new[] { nameof(vc_fileguid) });

    }

    public class FileStore : Entity<FileStoreDef>
    {
        public FileStore() : base() { }
        public FileStore(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }


}
