import { CommonFlags as c, DefaultColumns, EntityColumn, EntityDefinition } from "../../Entity";

const columns = () => ({
    c_import_process_id: new EntityColumn<string>({ name: 'c_import_process_id', type: 'char', length: 20, flags: c.PK, prompt: 'Import process ID' }),
    c_import_process_error_id: new EntityColumn<string>({ name: 'c_import_process_error_id', type: 'char', length: 20, flags: c.PKAutonum, prompt: 'Import process Error ID' }),
    i_row_number: new EntityColumn<number>({ name: 'i_row_number', type: 'int', flags: c.Edit, prompt: 'Row #' }),
    vc_error: new EntityColumn<string>({ name: 'vc_error', type: 'varchar', length: 0, flags: c.Edit, prompt: 'Error' }),
    ...DefaultColumns()
});

const views = () => ({
    ipe_brwStandard: { name: 'ipe_brwStandard', keyMappings: { c_import_process_error_id: 0 } }
});


export class ImportProcessErrorsDef extends EntityDefinition {
    columns = columns();
    views = views();

    constructor() {
        super('ImportProcessErrors');
    }
}
