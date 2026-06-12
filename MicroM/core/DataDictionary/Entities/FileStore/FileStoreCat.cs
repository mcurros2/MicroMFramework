using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class FileStoreCatDef : EntityDefinition
{
    public FileStoreCatDef() : base("fscc", nameof(FileStoreCat)) { SQLCreationOptions = SQLCreationOptionsMetadata.WithIUpdateAndIDrop; }

    public readonly Column<string> c_file_id = Column<string>.PK();
    public readonly Column<string> c_category_id = Column<string>.PK();
    public readonly Column<string> c_categoryvalue_id = Column<string>.FK();

    public readonly ViewDefinition fscc_brwStandard = new(nameof(c_file_id));

    public readonly EntityForeignKey<CategoriesValues, FileStoreCat> FKFileStore = new();
    public readonly EntityForeignKey<CategoriesValues, FileStoreCat> FKCategoriesValues = new();
}

public class FileStoreCat : Entity<FileStoreCatDef>
{
    public FileStoreCat() : base() { }
    public FileStoreCat(string? schema_name) : base(schema_name) { }
    public FileStoreCat(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }
}
