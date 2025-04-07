using MicroM.Core;
using MicroM.Data;
using MicroM.DataDictionary.StatusDefs;
using MicroM.Extensions;
using MicroM.Web.Services;

namespace MicroM.DataDictionary
{

    public class ImportProcessDef : EntityDefinition
    {
        public ImportProcessDef() : base("ipr", nameof(ImportProcess)) { }

        public readonly Column<string> c_import_process_id = Column<string>.PK(autonum: true);
        public readonly Column<string> c_fileprocess_id = Column<string>.FK();

        // This is the entity_name
        public readonly Column<string> vc_assemblytypename = Column<string>.Text(size: 2048, column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.FK);
        public readonly Column<string?> vc_import_procname = Column<string?>.Text(size: 2048, column_flags: ColumnFlags.Insert | ColumnFlags.Update | ColumnFlags.FK, nullable: true);

        public readonly Column<string> c_import_status_id = Column<string>.EmbedStatus(nameof(ImportStatus));

        public readonly Column<string?> vc_fileguid = Column<string?>.Text(nullable: true, fake: true);

        public readonly ViewDefinition ipr_brwStandard = new(nameof(c_import_process_id), nameof(vc_assemblytypename));

        public readonly ProcedureDefinition ipr_UpdateStatus = new(nameof(c_import_process_id), nameof(c_import_status_id), nameof(webusr));

        public readonly EntityForeignKey<FileStoreProcess, ImportProcess> FKImportProcessProcess = new();

    }

    public class ImportProcess : Entity<ImportProcessDef>
    {
        public ImportProcess() : base() { }
        public ImportProcess(IEntityClient ec, IMicroMEncryption? encryptor = null) : base(ec, encryptor) { }

        public async Task UpdateStatus(string status, CancellationToken ct)
        {
            this.Def.ipr_UpdateStatus.SetParmsValues(this.Def.Columns);
            this.Def.ipr_UpdateStatus.Parms[this.Def.c_import_status_id.Name].ValueObject = status;
            await this.ExecuteProc(ct, this.Def.ipr_UpdateStatus, set_parms_from_columns: false);
        }

    }


}
