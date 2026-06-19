using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.Entities;
using MicroM.Web.Services;

namespace MicroM.Configuration.Entities;

public class ApplicationsCatDef : EntityDefinition
{
    public ApplicationsCatDef() : base("appc", nameof(ApplicationsCat)) { }

    public readonly Column<string> c_application_id = Column<string>.PK();
    public readonly Column<string> c_category_id = Column<string>.PK();
    public readonly Column<string> c_categoryvalue_id = Column<string>.FK();

    public readonly ViewDefinition appc_brwStandard = new(nameof(c_application_id));

    public readonly EntityForeignKey<Applications, ApplicationsCat> FKApplicationsCat = new();
    public readonly EntityForeignKey<CategoriesValues, ApplicationsCat> FKCategories = new();
}

public class ApplicationsCat : Entity<ApplicationsCatDef>
{
    public ApplicationsCat() : base() { }
    public ApplicationsCat(string? schema_name) : base(schema_name) { }
    public ApplicationsCat(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
