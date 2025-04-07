import { DefaultColumns, Entity, EntityColumn, EntityColumnFlags, EntityDefinition } from "Entity";
import { MicroMClient } from "client";


const columns = () =>
(
    {
        c_fileprocess_id: new EntityColumn<string>({ name: 'c_fileprocess_id', type: 'char', length: 20, flags: EntityColumnFlags.pk, prompt: 'File', defaultValue: '' }),
        vc_fileguid: new EntityColumn<string>({ name: 'vc_fileguid', type: 'varchar', length: 80, flags: EntityColumnFlags.None, prompt: 'File GUID', defaultValue: '' }),

        ...DefaultColumns()
    }
)

export class AvatarUploaderTestEntityDef extends EntityDefinition {

    columns = columns();
    views = {};
    lookups = {};

    constructor() {
        super('AvatarUploaderTestEntity');
    }
}

export class AvatarUploaderTestEntity extends Entity<AvatarUploaderTestEntityDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new AvatarUploaderTestEntityDef(), parentKeys);
    }
}