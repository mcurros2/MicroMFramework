using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class CategoriesDef : EntityDefinition
{
    public CategoriesDef() : base("cat", nameof(Categories)) { }

    public readonly Column<string> c_category_id = Column<string>.PK();
    public readonly Column<string> vc_description = Column<string>.Text();
    public readonly Column<string> vc_json_categories = Column<string>.Text(size: 0, fake: true, column_flags: ColumnFlags.None);
    public readonly ProcedureDefinition cat_InitializeCategories = new(nameof(vc_json_categories), nameof(webusr));

    public readonly ViewDefinition cat_brwStandard = new(nameof(c_category_id));

}

public class Categories : Entity<CategoriesDef>
{
    public Categories() : base() { }
    public Categories(string? schema_name) : base(schema_name) { }
    public Categories(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
