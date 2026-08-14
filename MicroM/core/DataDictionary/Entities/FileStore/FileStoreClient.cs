using MicroM.Configuration;
using MicroM.Configuration.CategoriesDefinitions;
using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.StatusDefinitions;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class FileStoreClientDef : EntityDefinition
{
    public FileStoreClientDef() : base("fcc", nameof(FileStoreClient)) { Fake = true; }

    public readonly Column<string> vc_fileguid = Column<string>.Text(column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.Delete | ColumnFlags.Get | ColumnFlags.PK);
    public readonly Column<string> c_fileprocess_id = Column<string>.FK();
    public readonly Column<string> vc_filename = Column<string>.Text();
    public readonly Column<string> vc_filefolder = Column<string>.Char(size: 6);
    public readonly Column<long> bi_filesize = new();
    public readonly Column<string?> vc_file_tag = Column<string?>.Text(nullable: true);

    public readonly Column<string> c_fileuploadstatus_id = Column<string>.EmbedStatus(nameof(FileUpload));
    public readonly Column<string> c_filestoragetype_id = Column<string>.EmbedCategory(nameof(FileStorageTypes));

    public readonly ViewDefinition fcc_brwFiles = new(nameof(vc_fileguid), nameof(c_fileprocess_id));
}

public class FileStoreClient : Entity<FileStoreClientDef>
{
    public FileStoreClient() : base() { }
    public FileStoreClient(string? schema_name) : base(schema_name) { }
    public FileStoreClient(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

    public override async Task<DBStatusResult> DeleteData(
        CancellationToken ct, bool throw_dbstat_exception = false, MicroMOptions? options = null, Dictionary<string, object>? server_claims = null,
        IWebAPIServices? api = null, string? app_id = null)
    {
        if (api == null || string.IsNullOrWhiteSpace(app_id))
        {
            return DBStatusResult.FailedStatus([new(DBStatusCodes.Error, "File deletion requires the web API services and application ID.")]);
        }

        var app = api.app_config.GetAppConfiguration(app_id);
        if (app == null)
        {
            return DBStatusResult.FailedStatus([new(DBStatusCodes.Error, $"Application '{app_id}' was not found.")]);
        }

        return await api.upload.DeleteFile(app, Def.vc_fileguid.Value, Client, ct);
    }

}
