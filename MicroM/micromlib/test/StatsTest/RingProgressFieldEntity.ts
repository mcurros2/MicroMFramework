import { DefaultColumns, Entity, EntityColumn, EntityColumnFlags, EntityDefinition } from "Entity";
import { MicroMClient } from "client";


const columns = () =>
(
    {
        c_ring_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: EntityColumnFlags.pk, prompt: 'File', defaultValue: '' }),
        i_presentes: new EntityColumn<number>({ name: 'i_presentes', type: 'int', flags: EntityColumnFlags.edit, prompt: 'Presentes', value: 120, description: 'Personal Presente' }),
        i_ausentes: new EntityColumn<number>({ name: 'i_ausentes', type: 'int', flags: EntityColumnFlags.edit, prompt: 'Ausentes', value: 15, description: 'Personal Ausente' }),
        i_demorados: new EntityColumn<number>({ name: 'i_demorados', type: 'int', flags: EntityColumnFlags.edit, prompt: 'Demorados', value: 22, description: 'Personal Demorado' }),
        ...DefaultColumns()
    }
)

export class RingProgressFieldEntityDef extends EntityDefinition {

    columns = columns();
    views = {};
    lookups = {};

    constructor() {
        super('RingProgressFieldEntity');
    }
}

export class RingProgressFieldEntity extends Entity<RingProgressFieldEntityDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new RingProgressFieldEntityDef(), parentKeys);
    }
}