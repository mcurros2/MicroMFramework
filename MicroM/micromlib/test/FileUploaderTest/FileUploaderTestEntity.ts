import { DefaultColumns, Entity, EntityColumn, EntityColumnFlags, EntityDefinition } from "Entity";
import { MicroMClient } from "client";


const columns = () =>
(
    {
        c_fileprocess_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: EntityColumnFlags.pk, prompt: 'File', defaultValue: '' }),
        i_order: new EntityColumn<number>({ name: 'i_order', type: 'int', flags: EntityColumnFlags.edit, prompt: 'Order', defaultValue: 0, description: 'Order of the uploaded file' }),
        d_week_date: new EntityColumn<Date>({ name: 'd_week_date', type: 'datetime', length: 20, flags: EntityColumnFlags.edit, prompt: 'Week', description: 'Select a week' }),
        ...DefaultColumns()
    }
)

export class FileUploaderTestEntityDef extends EntityDefinition {

    columns = columns();
    views = {};
    lookups = {};

    constructor() {
        super('FileUploaderTestEntity');
    }
}

export class FileUploaderTestEntity extends Entity<FileUploaderTestEntityDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new FileUploaderTestEntityDef(), parentKeys);
    }
}