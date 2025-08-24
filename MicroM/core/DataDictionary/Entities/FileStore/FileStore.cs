
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.StatusDefs;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Definition of the <c>FileStore</c> entity used to persist uploaded files and
    /// their metadata within the system.
    /// </summary>
    public class FileStoreDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStoreDef"/> class.
        /// </summary>
        public FileStoreDef() : base("fst", nameof(FileStore)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        /// <summary>
        /// Primary identifier of the stored file.
        /// </summary>
        public readonly Column<string> c_file_id = Column<string>.PK(autonum: true);

        /// <summary>
        /// Identifier of the process that generated the file.
        /// </summary>
        public readonly Column<string> c_fileprocess_id = Column<string>.FK();

        /// <summary>
        /// Name of the file as stored on disk.
        /// </summary>
        public readonly Column<string> vc_filename = Column<string>.Text();

        /// <summary>
        /// Folder portion of the file path used for storage.
        /// </summary>
        public readonly Column<string> vc_filefolder = Column<string>.Char(size: 6);

        /// <summary>
        /// Globally unique identifier associated with the file.
        /// </summary>
        public readonly Column<string> vc_fileguid = Column<string>.Text(column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.FK);

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public readonly Column<long> bi_filesize = new();

        /// <summary>
        /// Status identifier tracking the upload progress of the file.
        /// </summary>
        public readonly Column<string> c_fileuploadstatus_id = Column<string>.EmbedStatus(nameof(FileUpload));

        /// <summary>
        /// Standard browse view definition.
        /// </summary>
        public readonly ViewDefinition fst_brwStandard = new(nameof(c_file_id));

        /// <summary>
        /// Browse view for retrieving files by process.
        /// </summary>
        public readonly ViewDefinition fst_brwFiles = new(nameof(c_file_id), nameof(c_fileprocess_id));

        /// <summary>
        /// Procedure used to get a file by its GUID value.
        /// </summary>
        public readonly ProcedureDefinition fst_getByGUID = new(new[] { nameof(vc_fileguid) });

        /// <summary>
        /// Foreign key to the associated <see cref="FileStoreProcess"/> record.
        /// </summary>
        public readonly EntityForeignKey<FileStoreProcess, FileStore> FKFileStoreProcess = new();

        /// <summary>
        /// Enforces uniqueness of <see cref="vc_fileguid"/> values.
        /// </summary>
        public readonly EntityUniqueConstraint UCFileStore = new(keys: new[] { nameof(vc_fileguid) });
    }

    /// <summary>
    /// Runtime entity for interacting with stored files.
    /// </summary>
    public class FileStore : Entity<FileStoreDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStore"/> class.
        /// </summary>
        public FileStore() : base() { }

        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        /// <param name="ec">Database client used to access persistence layer.</param>
        /// <param name="encryptor">Optional encryptor for sensitive data.</param>
        public FileStore(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
    }
}
