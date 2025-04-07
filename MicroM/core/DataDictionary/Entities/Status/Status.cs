using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;
using System.Data;

namespace MicroM.DataDictionary
{
    public class StatusDef : EntityDefinition
    {
        public StatusDef() : base("sta", nameof(Status)) { }

        public readonly Column<string> c_status_id = Column<string>.PK();
        public readonly Column<string> vc_description = new(sql_type: SqlDbType.VarChar, size: 255);

        public ViewDefinition sta_brwStandard { get; private set; } = new(nameof(c_status_id));

    }

    public class Status : Entity<StatusDef>
    {
        public Status() : base() { }

        public Status(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }

}
