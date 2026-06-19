using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class MicromUsersCatDef : EntityDefinition
{
    public MicromUsersCatDef() : base("usrc", nameof(MicromUsersCat)) { }

    public readonly Column<string> c_user_id = Column<string>.PK();
    public readonly Column<string> c_category_id = Column<string>.PK();
    public readonly Column<string> c_categoryvalue_id = Column<string>.FK();

    public readonly ViewDefinition usrc_brwStandard = new(nameof(c_user_id));

    public readonly EntityForeignKey<MicromUsers, MicromUsersCat> FKMicromUsers = new();
    public readonly EntityForeignKey<CategoriesValues, MicromUsersCat> FKCategories = new();

}

public class MicromUsersCat : Entity<MicromUsersCatDef>
{
    public MicromUsersCat() : base() { }
    public MicromUsersCat(string? schema_name) : base(schema_name) { }
    public MicromUsersCat(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}