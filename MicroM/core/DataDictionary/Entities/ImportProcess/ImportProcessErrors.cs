
using MicroM.Core;
using MicroM.Data;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class ImportProcessErrorsDef : EntityDefinition
    {
        public ImportProcessErrorsDef() : base("ipe", nameof(ImportProcessErrors)) { }

        public readonly Column<string> c_import_process_id = Column<string>.PK();
        public readonly Column<string> c_import_process_error_id = Column<string>.PK(autonum: true);

        public readonly Column<string> vc_error = Column<string>.Text(size: 0);


        public readonly ViewDefinition ipe_brwStandard = new(nameof(c_import_process_id), nameof(c_import_process_error_id));

        public readonly EntityForeignKey<ImportProcess, ImportProcessErrors> FKImportProcess = new();

    }

    public class ImportProcessErrors : Entity<ImportProcessErrorsDef>
    {
        public ImportProcessErrors() : base() { }
        public ImportProcessErrors(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

    }


}
