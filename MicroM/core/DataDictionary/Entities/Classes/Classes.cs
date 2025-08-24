using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    /// <summary>
    /// Entity definition for class records associated with objects.
    /// </summary>
    public class ClassesDef : EntityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassesDef"/> class.
        /// </summary>
        public ClassesDef() : base("cla", nameof(Classes)) { }

        /// <summary>
        /// Identifier of the object.
        /// </summary>
        public readonly Column<string> c_object_id = Column<string>.PK();
        /// <summary>
        /// Identifier of the class.
        /// </summary>
        public readonly Column<string> c_class_id = Column<string>.PK(autonum: true);
        /// <summary>
        /// Name of the class.
        /// </summary>
        public readonly Column<string> vc_classname = new(sql_type: SqlDbType.VarChar, size: 255);

        /// <summary>
        /// Standard browse view for classes.
        /// </summary>
        public ViewDefinition cla_brwStandard { get; private set; } = new(nameof(c_object_id), nameof(c_class_id));
        //protected override void DefineViews()
        //{
        //    cla_brwStandard = new ViewDefinition(true,
        //        new ViewParm(c_object_id),
        //        new ViewParm(c_class_id, column_mapping: 0, browsing_key: true)
        //        );
        //}


        /// <summary>
        /// Foreign key relationship to objects.
        /// </summary>
        public readonly EntityForeignKey<Objects, Classes> FKObjects = new();

        /// <summary>
        /// Unique constraint enforcing class name uniqueness.
        /// </summary>
        public readonly EntityUniqueConstraint UNClassName = new(keys: nameof(vc_classname));

    }

    /// <summary>
    /// Represents a class associated with an object.
    /// </summary>
    public class Classes : Entity<ClassesDef>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Classes"/> class.
        /// </summary>
        public Classes() : base() { }

        /// <summary>
        /// Initializes a new instance using the specified entity client and optional encryptor.
        /// </summary>
        public Classes(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
