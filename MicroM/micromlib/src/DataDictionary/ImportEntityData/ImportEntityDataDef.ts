import { DefaultColumns, EntityColumn, EntityDefinition, CommonFlags as c } from "../../Entity";
import { MicroMClient, ValuesObject } from "../../client";
import { FileStoreProcess } from "../FileStoreProcess";

const columns = () =>
(
    {
        c_fileprocess_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: c.FK, prompt: 'Fileprocess Id' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        ipr_brwStandard: { name: 'ipd_brwStandard', keyMappings: {c_import_process: 0 } }
    }
)

const lookups = () =>
(
    {
        'FileStoreProcess': {
            name: 'FileStoreProcess',
            viewMapping: { keyIndex: 0, descriptionIndex: 1 },
            entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new FileStoreProcess(client, parentKeys)
        },

    }
)

export class ImportEntityDataDef extends EntityDefinition {

    columns = columns();
    views = views();
    lookups = lookups();

    constructor() {
        super('ImportEntityData');
    }
}