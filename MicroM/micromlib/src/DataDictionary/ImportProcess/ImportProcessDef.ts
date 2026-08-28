import { CommonFlags as c, DefaultColumns, EntityColumn, EntityColumnFlags, EntityDefinition } from "../../Entity";
import { ACTDownloadImportedFile } from "./ACTDownloadImportedFile";
import { ACTImportData } from "./ACTImportData";

const columns = () => ({
    c_import_process_id: new EntityColumn<string>({ name: 'c_import_process_id', type: 'char', length: 20, flags: c.PKAutonum, prompt: 'Import process ID' }),
    c_fileprocess_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: c.FK, prompt: 'File process ID' }),
    i_total_records: new EntityColumn<number>({ name: 'i_total_records', type: 'int', flags: c.Edit, prompt: 'Processed' }),
    i_errors: new EntityColumn<number>({ name: 'i_errors', type: 'int', flags: c.Edit, prompt: 'Errors' }),
    vc_assemblytypename: new EntityColumn<string>({ name: 'vc_assemblytypename', type: 'varchar', length: 2048, flags: c.FK, prompt: 'Destination entity' }),
    vc_import_procname: new EntityColumn<string>({ name: 'vc_import_procname', type: 'varchar', length: 2048, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Import procedure' }),
    c_import_status_id: new EntityColumn<string>({ name: 'c_import_status_id', type: 'char', length: 20, flags: c.Edit, prompt: 'Import status' }),
    vc_fileguid: new EntityColumn<string>({ name: 'vc_fileguid', type: 'varchar', length: 255, flags: c.FK, prompt: 'File GUID' }),
    ...DefaultColumns()
});

const views = () => ({
    ipr_brwStandard: { name: 'ipr_brwStandard', keyMappings: { c_import_process_id: 0, vc_fileguid: 7 } }
});

const clientActions = () => ({
    ACTImportData,
    ACTDownloadImportedFile,
});

export class ImportProcessDef extends EntityDefinition {
    columns = columns();
    views = views();
    clientActions = clientActions();

    constructor() {
        super('ImportProcess');
    }
}
