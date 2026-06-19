using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary.Entities;

public class ClassesDef : EntityDefinition
{
    public ClassesDef() : base("cla", nameof(Classes)) { }

    public readonly Column<string> c_object_id = Column<string>.PK();
    public readonly Column<string> c_class_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_classname = Column<string>.Text();

    public readonly ViewDefinition cla_brwStandard = new(nameof(c_object_id), nameof(c_class_id));

    public readonly EntityForeignKey<Objects, Classes> FKObjects = new();

    public readonly EntityUniqueConstraint UNClassName = new(keys: nameof(vc_classname));

}

public class Classes : Entity<ClassesDef>
{
    public Classes() : base() { }
    public Classes(string? schema_name) : base(schema_name) { }

    public Classes(IEntityClient ec, IMicroMEncryption? encryptor = null, string? schema_name = null) : base(ec, encryptor, schema_name) { }

}
