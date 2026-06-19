import { CommonFlags as c, DefaultColumns, EntityColumn, EntityDefinition } from "../../Entity";
import { ACTMicromGetGeneratedCode } from "./ACTMicromGeneratedCode";

const columns = () =>
(
    {
        vc_entity_name: new EntityColumn<string>({ name: 'vc_entity_name', type: 'varchar', length: 255, flags: c.PK, prompt: 'Entity Name' }),
        vc_entity_type: new EntityColumn<string>({ name: 'vc_entity_type', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Entity Type' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        mty_brwStandard: { name: 'mty_brwStandard', keyMappings: { vc_entity_name: 0 } }
    }
)

const clientActions = () => (
    {
        ACTMicromGetGeneratedCode
    }
)

export class MicromEntitiesTypesDef extends EntityDefinition {

    columns = columns();
    views = views();
    clientActions = clientActions();

    constructor() {
        super('MicromEntitiesTypes');
    }
}