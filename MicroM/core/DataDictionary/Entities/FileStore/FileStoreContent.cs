using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class FileStoreContentDef : EntityDefinition
{
    public FileStoreContentDef() : base("fsc", nameof(FileStoreContent)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_file_id = Column<string>.PK();
    public readonly Column<Stream> vb_file_content = Column<Stream>.BinaryStream();

    public readonly ProcedureDefinition fsc_GetFileContent = new(readonly_locks: true, nameof(c_file_id));
    public readonly ProcedureDefinition fsc_InsertFileContent = new(nameof(c_file_id), nameof(vb_file_content), nameof(webusr));

    public readonly EntityForeignKey<FileStore, FileStoreContent> FKFileStore = new();
}

public class FileStoreContent : Entity<FileStoreContentDef>
{
    public FileStoreContent() : base() { }
    public FileStoreContent(string? schema_name) : base(schema_name) { }
    public FileStoreContent(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    public static async Task StoreFile(IEntityClient ec, ApplicationOption app, string file_id, Stream file_content, CancellationToken ct)
    {
        var fsc = new FileStoreContent(ec, schema_name: app.SchemaConfiguration.DDSchema);
        fsc.Def.c_file_id.Value = file_id;
        fsc.Def.vb_file_content.Value = file_content;
        await fsc.ExecuteProcNonQuery(fsc.Def.fsc_InsertFileContent, ct);
    }


    public static async Task<Stream> GetFileStream(IEntityClient ec, ApplicationOption app, string file_id, CancellationToken ct)
    {
        var fsc = new FileStoreContent(ec, schema_name: app.SchemaConfiguration.DDSchema);
        fsc.Def.c_file_id.Value = file_id;
        return await fsc.ExecuteProcSingleColumn<Stream>(fsc.Def.fsc_GetFileContent, ct) ?? Stream.Null;
    }
}
