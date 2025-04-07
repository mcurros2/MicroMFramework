import { DefaultColumns, EntityColumn, EntityDefinition, CommonFlags as c } from "../../Entity";

const columns = () =>
(
    {
        c_status_id: new EntityColumn<string>({ name: 'c_status_id', type: 'char', length: 20, flags: c.PK, prompt: 'Status Id' }),
        c_statusvalue_id: new EntityColumn<string>({ name: 'c_statusvalue_id', type: 'char', length: 20, flags: c.PK, prompt: 'Statusvalue Id' }),
        vc_description: new EntityColumn<string>({ name: 'vc_description', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Description' }),
        bt_initial_value: new EntityColumn<boolean>({ name: 'bt_initial_value', type: 'bit', flags: c.Edit, prompt: 'Initial Value' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        stv_brwStandard: { name: 'stv_brwStandard', keyMappings: { c_statusvalue_id: 0 } }
    }
)

export class StatusValuesDef extends EntityDefinition {

    columns = columns();
    views = views();

    constructor() {
        super('StatusValues');
    }
}