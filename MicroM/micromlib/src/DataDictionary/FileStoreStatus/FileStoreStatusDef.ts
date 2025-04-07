import { DefaultColumns, EntityColumn, EntityDefinition, CommonFlags as c } from "../../Entity";

const columns = () =>
(
    {
        c_file_id: new EntityColumn<string>({ name: 'c_file_id', type: 'char', length: 20, flags: c.PK, prompt: 'File Id' }),
        c_status_id: new EntityColumn<string>({ name: 'c_status_id', type: 'char', length: 20, flags: c.PK, prompt: 'Status Id' }),
        c_statusvalue_id: new EntityColumn<string>({ name: 'c_statusvalue_id', type: 'char', length: 20, flags: c.FK, prompt: 'Statusvalue Id' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        fsts_brwStandard: { name: 'fsts_brwStandard', keyMappings: { c_status_id: 0 } }
    }
)


export class FileStoreStatusDef extends EntityDefinition {

    columns = columns();
    views = views();


    constructor() {
        super('FileStoreStatus');
    }
}