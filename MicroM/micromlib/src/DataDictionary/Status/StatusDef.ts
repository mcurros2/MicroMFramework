import { DefaultColumns, EntityColumn, EntityDefinition, EntityColumnFlags, CommonFlags as c } from "../../Entity";

const columns = () =>
(
    {
        c_status_id: new EntityColumn<string>({ name: 'c_status_id', type: 'char', length: 20, flags: c.PK, prompt: 'Status Id' }),
        vc_description: new EntityColumn<string>({ name: 'vc_description', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Description' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        sta_brwStandard: { name: 'sta_brwStandard', keyMappings: { c_status_id: 0 } }
    }
)

export class StatusDef extends EntityDefinition {

    columns = columns();
    views = views();

    constructor() {
        super('Status');
    }
}