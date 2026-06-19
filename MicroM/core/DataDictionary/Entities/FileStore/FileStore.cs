using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.StatusDefinitions;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary.Entities;

public class FileStoreDef : EntityDefinition
{
    public FileStoreDef() : base("fst", nameof(FileStore)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_file_id = Column<string>.PK(autonum: true);
    public readonly Column<string> c_fileprocess_id = Column<string>.FK();
    public readonly Column<string> vc_filename = Column<string>.Text();
    public readonly Column<string> vc_filefolder = Column<string>.Char(size: 6);
    public readonly Column<string> vc_fileguid = Column<string>.Text(column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.FK);
    public readonly Column<long> bi_filesize = new();
    public readonly Column<string?> vc_file_tag = Column<string?>.Text(nullable: true);

    public readonly Column<string> c_fileuploadstatus_id = Column<string>.EmbedStatus(nameof(FileUpload));
    public readonly Column<string> c_filestoragetype_id = Column<string>.EmbedCategory(nameof(FileStorageTypes));

    public readonly ViewDefinition fst_brwStandard = new(nameof(c_file_id));
    public readonly ViewDefinition fst_brwFiles = new(nameof(c_file_id), nameof(c_fileprocess_id));

    public readonly ProcedureDefinition fst_getByGUID = new([nameof(vc_fileguid)]);

    public readonly EntityForeignKey<FileStoreProcess, FileStore> FKFileStoreProcess = new();
    public readonly EntityUniqueConstraint UCFileStore = new(keys: [nameof(vc_fileguid)]);

}

public class FileStore : Entity<FileStoreDef>
{
    public FileStore() : base() { }
    public FileStore(string? schema_name) : base(schema_name) { }
    public FileStore(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }


    public static async Task UpdateStatus(IEntityClient ec, string schema, string file_id, string statusValue, CancellationToken ct)
    {
        bool should_close = (ec.ConnectionState != ConnectionState.Open);
        try
        {
            await ec.Connect(ct);

            var fileStoreSatus = new FileStoreStatus(ec, schema_name: schema);
            fileStoreSatus.Def.c_file_id.Value = file_id;
            fileStoreSatus.Def.c_status_id.Value = nameof(FileUpload);
            fileStoreSatus.Def.c_statusvalue_id.Value = statusValue;
            await fileStoreSatus.UpdateData(ct, true);
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }
}
