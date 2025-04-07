using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    public class ViewParmsDef : EntityDefinition
    {
        public ViewParmsDef() : base("vip", nameof(ViewParms)) { }

        public readonly Column<string> c_object_id = Column<string>.PK();
        public readonly Column<int> c_proc_id = Column<int>.PK();
        public readonly Column<int> c_viewparm_id = Column<int>.PK(autonum: true);
        public readonly Column<string> vc_parmname = new(sql_type: SqlDbType.VarChar, size: 255);
        public readonly Column<int?> i_columnmapping = new();
        public readonly Column<string?> vc_compoundgroup = new(sql_type: SqlDbType.VarChar, size: 80, nullable: true);
        public readonly Column<int?> i_compoundposition = new();
        public readonly Column<bool> bt_compoundkey = new();
        public readonly Column<bool> bt_browsingkey = new();

        public ViewDefinition vip_brwStandard { get; private set; } = new(nameof(c_object_id), nameof(c_proc_id), nameof(c_viewparm_id));

        public readonly EntityForeignKey<Procs, ViewParms> FKObjects = new();

    }

    public class ViewParms : Entity<ViewParmsDef>
    {
        public ViewParms() : base() { }

        public ViewParms(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
