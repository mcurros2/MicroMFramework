
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Definition linking a file to its current status value.
    /// </summary>
    public class FileStoreStatusDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStoreStatusDef"/> class.
        /// </summary>
        public FileStoreStatusDef() : base("fsts", nameof(FileStoreStatus)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        /// <summary>
        /// Identifier of the file.
        /// </summary>
        public readonly Column<string> c_file_id = Column<string>.PK();

        /// <summary>
        /// Identifier of the status set.
        /// </summary>
        public readonly Column<string> c_status_id = Column<string>.PK();

        /// <summary>
        /// Identifier of the specific status value.
        /// </summary>
        public readonly Column<string> c_statusvalue_id = Column<string>.FK();

        /// <summary>
        /// Browse view exposing file and status keys.
        /// </summary>
        public ViewDefinition fsts_brwStandard { get; private set; } = new(nameof(c_file_id), nameof(c_status_id));

        /// <summary>
        /// Foreign key to the associated <see cref="FileStore"/> record.
        /// </summary>
        public readonly EntityForeignKey<FileStore, FileStoreStatus> FKFileStoreStatus = new();

        /// <summary>
        /// Foreign key to the related <see cref="StatusValues"/> record.
        /// </summary>
        public readonly EntityForeignKey<StatusValues, FileStoreStatus> FKStatus = new();
    }

    /// <summary>
    /// Runtime entity representing the status of a stored file.
    /// </summary>
    public class FileStoreStatus : Entity<FileStoreStatusDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStoreStatus"/> class.
        /// </summary>
        public FileStoreStatus() : base() { }

        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        /// <param name="ec">Database client used for persistence.</param>
        /// <param name="encryptor">Optional encryptor for sensitive columns.</param>
        public FileStoreStatus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
    }
}
