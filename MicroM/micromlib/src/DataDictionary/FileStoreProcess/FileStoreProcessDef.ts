import { CommonFlags as c, DefaultColumns, EntityColumn, EntityDefinition } from "../../Entity";


const columns = () =>
(
    {
        c_fileprocess_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: c.PKAutonum, prompt: 'Fileprocess Id' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        fsp_brwStandard: { name: 'fsp_brwStandard', keyMappings: { c_fileprocess_id: 0 } }
    }
)

export class FileStoreProcessDef extends EntityDefinition {

    columns = columns();
    views = views();

    constructor() {
        super('FileStoreProcess');
    }
}