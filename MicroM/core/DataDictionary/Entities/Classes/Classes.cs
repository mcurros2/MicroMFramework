using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    public class ClassesDef : EntityDefinition
    {
        public ClassesDef() : base("cla", nameof(Classes)) { }

        public readonly Column<string> c_object_id = Column<string>.PK();
        public readonly Column<string> c_class_id = Column<string>.PK(autonum: true);
        public readonly Column<string> vc_classname = new(sql_type: SqlDbType.VarChar, size: 255);

        public ViewDefinition cla_brwStandard { get; private set; } = new(nameof(c_object_id), nameof(c_class_id));
        //protected override void DefineViews()
        //{
        //    cla_brwStandard = new ViewDefinition(true,
        //        new ViewParm(c_object_id),
        //        new ViewParm(c_class_id, column_mapping: 0, browsing_key: true)
        //        );
        //}


        public readonly EntityForeignKey<Objects, Classes> FKObjects = new();

        public readonly EntityUniqueConstraint UNClassName = new(keys: nameof(vc_classname));

    }

    public class Classes : Entity<ClassesDef>
    {
        public Classes() : base() { }

        public Classes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
