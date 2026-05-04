import { CommonFlags as c, DefaultColumns, EntityColumn, EntityDefinition } from "../../Entity";

const columns = () =>
(
    {
        vc_classname: new EntityColumn<string>({ name: 'vc_classname', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Classname' }),
        vc_table: new EntityColumn<string>({ name: 'vc_table', type: 'varchar', flags: c.Edit, prompt: 'Table' }),
        vc_indexes: new EntityColumn<string>({ name: 'vc_indexes', type: 'varchar', flags: c.Edit, prompt: 'Indexes' }),
        vc_sp_get: new EntityColumn<string>({ name: 'vc_sp_get', type: 'varchar', flags: c.Edit, prompt: 'Sp Get' }),
        vc_sp_update: new EntityColumn<string>({ name: 'vc_sp_update', type: 'varchar', flags: c.Edit, prompt: 'Sp Update' }),
        vc_sp_iupdate: new EntityColumn<string>({ name: 'vc_sp_iupdate', type: 'varchar', flags: c.Edit, prompt: 'Sp Iupdate' }),
        vc_sp_updatei: new EntityColumn<string>({ name: 'vc_sp_updatei', type: 'varchar', flags: c.Edit, prompt: 'Sp Updatei' }),
        vc_sp_drop: new EntityColumn<string>({ name: 'vc_sp_drop', type: 'varchar', flags: c.Edit, prompt: 'Sp Drop' }),
        vc_sp_idrop: new EntityColumn<string>({ name: 'vc_sp_idrop', type: 'varchar', flags: c.Edit, prompt: 'Sp Idrop' }),
        vc_sp_dropi: new EntityColumn<string>({ name: 'vc_sp_dropi', type: 'varchar', flags: c.Edit, prompt: 'Sp Dropi' }),
        vc_sp_lookup: new EntityColumn<string>({ name: 'vc_sp_lookup', type: 'varchar', flags: c.Edit, prompt: 'Sp Lookup' }),
        vc_sp_brwStandard: new EntityColumn<string>({ name: 'vc_sp_brwStandard', type: 'varchar', flags: c.Edit, prompt: 'Sp Brwstandard' }),
        vc_custom_procs: new EntityColumn<string>({ name: 'vc_custom_procs', type: 'varchar', flags: c.Edit, prompt: 'Custom Procs' }),
        vc_react_definition: new EntityColumn<string>({ name: 'vc_react_definition', type: 'varchar', flags: c.Edit, prompt: 'React Definition' }),
        vc_react_entity: new EntityColumn<string>({ name: 'vc_react_entity', type: 'varchar', flags: c.Edit, prompt: 'React Entity' }),
        vc_react_categories: new EntityColumn<string>({ name: 'vc_react_categories', type: 'varchar', flags: c.Edit, prompt: 'React Categories' }),
        vc_react_form: new EntityColumn<string>({ name: 'vc_react_form', type: 'varchar', flags: c.Edit, prompt: 'React Form' }),
        ...DefaultColumns()
    }
)


export class MicromDeveloperToolsCodeGenDef extends EntityDefinition {

    columns = columns();

    constructor() {
        super('MicromDeveloperToolsCodeGen');
    }
}