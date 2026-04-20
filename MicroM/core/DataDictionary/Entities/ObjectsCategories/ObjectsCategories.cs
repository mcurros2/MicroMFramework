using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ObjectsCategoriesDef : EntityDefinition
{
    public ObjectsCategoriesDef() : base("oca", nameof(ObjectsCategories)) { }

    public readonly Column<string> c_object_id = Column<string>.PK();
    public readonly Column<string> c_category_id = Column<string>.PK();

    public readonly ViewDefinition oca_brwStandard = new(nameof(c_object_id), nameof(c_category_id));

    public readonly EntityForeignKey<Objects, ObjectsCategories> FKObjects = new();
    public readonly EntityForeignKey<Categories, ObjectsCategories> FKCategories = new();

}

public class ObjectsCategories : Entity<ObjectsCategoriesDef>
{
    public ObjectsCategories() : base() { }
    public ObjectsCategories(string? schema_name) : base(schema_name) { }
    public ObjectsCategories(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
