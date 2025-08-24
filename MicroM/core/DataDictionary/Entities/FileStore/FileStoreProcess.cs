
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Definition of processes that handle file storage operations.
    /// </summary>
    public class FileStoreProcessDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStoreProcessDef"/> class.
        /// </summary>
        public FileStoreProcessDef() : base("fsp", nameof(FileStoreProcess)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

        /// <summary>
        /// Identifier of the file processing operation.
        /// </summary>
        public readonly Column<string> c_fileprocess_id = Column<string>.PK(autonum: true);

        /// <summary>
        /// Browse view exposing file process identifiers.
        /// </summary>
        public ViewDefinition fsp_brwStandard { get; private set; } = new(nameof(c_fileprocess_id));
    }

    /// <summary>
    /// Runtime entity representing a file storage process.
    /// </summary>
    public class FileStoreProcess : Entity<FileStoreProcessDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStoreProcess"/> class.
        /// </summary>
        public FileStoreProcess() : base() { }

        /// <summary>
        /// Initializes a new instance with a database client and optional encryptor.
        /// </summary>
        /// <param name="ec">Database client used to interact with persistence.</param>
        /// <param name="encryptor">Optional encryptor for sensitive columns.</param>
        public FileStoreProcess(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }
    }
}
