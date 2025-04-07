
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class ImportProcessStatusDef : EntityDefinition
    {
        public ImportProcessStatusDef() : base("iprs", nameof(ImportProcessStatus)) { }

        public readonly Column<string> c_import_process_id = Column<string>.PK();
        public readonly Column<string> c_status_id = Column<string>.PK();
        public readonly Column<string> c_statusvalue_id = Column<string>.FK();

        public readonly ViewDefinition iprs_brwStandard = new(nameof(c_import_process_id), nameof(c_status_id));

        public readonly EntityForeignKey<ImportProcess, ImportProcessStatus> FKImportProcess = new();
        public readonly EntityForeignKey<StatusValues, ImportProcessStatus> FKStatus = new();

    }

    public class ImportProcessStatus : Entity<ImportProcessStatusDef>
    {
        public ImportProcessStatus() : base() { }
        public ImportProcessStatus(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }


}
