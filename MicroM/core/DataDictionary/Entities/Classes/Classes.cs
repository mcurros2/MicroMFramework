using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary.Entities;

public class ClassesDef : EntityDefinition
{
    public ClassesDef() : base("cla", nameof(Classes)) { }

    public readonly Column<string> c_object_id = Column<string>.PK();
    public readonly Column<string> c_class_id = Column<string>.PK(autonum: true);
    public readonly Column<string> vc_classname = new(sql_type: SqlDbType.VarChar, size: 255);

    public readonly ViewDefinition cla_brwStandard = new(nameof(c_object_id), nameof(c_class_id));

    public readonly EntityForeignKey<Objects, Classes> FKObjects = new();

    public readonly EntityUniqueConstraint UNClassName = new(keys: nameof(vc_classname));

}

public class Classes : Entity<ClassesDef>
{
    public Classes() : base() { }

    public Classes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

}
