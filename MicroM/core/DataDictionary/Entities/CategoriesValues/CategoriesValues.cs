using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary;

public class CategoriesValuesDef : EntityDefinition
{
    public CategoriesValuesDef() : base("cav", nameof(CategoriesValues)) { }

    public readonly Column<string> c_category_id = Column<string>.PK();
    public readonly Column<string> c_categoryvalue_id = Column<string>.PK();
    public readonly Column<string> vc_description = Column<string>.Text();

    public readonly ViewDefinition cav_brwStandard = new(nameof(c_category_id), nameof(c_categoryvalue_id));

    public readonly EntityForeignKey<Categories, CategoriesValues> FKCategories = new();
    public readonly EntityUniqueConstraint UNDescription = new(keys: [nameof(c_category_id), nameof(vc_description)]);

}

public class CategoriesValues : Entity<CategoriesValuesDef>
{
    public CategoriesValues() : base() { }
    public CategoriesValues(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
