using MicroM.Configuration;
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ApplicationsCatDef : EntityDefinition
{
    public ApplicationsCatDef() : base("appc", nameof(ApplicationsCat), schemaName: DataDefaults.DataDictionarySchema) { }

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
    public ApplicationsCat(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
