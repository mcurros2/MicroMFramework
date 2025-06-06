using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

public class CategoriesDef : EntityDefinition
{
    public CategoriesDef() : base("cat", nameof(Categories)) { }

    public readonly Column<string> c_category_id = Column<string>.PK();
    public readonly Column<string> vc_description = Column<string>.Text();

    public readonly ViewDefinition cat_brwStandard = new(nameof(c_category_id));

}

public class Categories : Entity<CategoriesDef>
{
    public Categories() : base() { }
    public Categories(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
