import { DefaultColumns, Entity, EntityColumn, EntityColumnFlags, EntityDefinition } from "Entity";
import { MicroMClient } from "client";


const columns = () =>
(
    {
        vc_street: new EntityColumn<string>({ name: 'vc_street', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Calle', defaultValue: '' }),
        vc_street_number: new EntityColumn<string>({ name: 'vc_street_number', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Altura/número', defaultValue: '' }),
        vc_department: new EntityColumn<string>({ name: 'vc_department', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Localidad', defaultValue: '' }),
        vc_city: new EntityColumn<string>({ name: 'vc_city', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Ciudad', defaultValue: '' }),
        vc_province: new EntityColumn<string>({ name: 'vc_province', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Provincia/estado', defaultValue: '' }),
        vc_country: new EntityColumn<string>({ name: 'vc_country', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'País', defaultValue: '' }),
        vc_country_code: new EntityColumn<string>({ name: 'vc_country_code', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'País', defaultValue: '' }),
        vc_postal_code: new EntityColumn<string>({ name: 'vc_postal_code', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Código postal', defaultValue: '' }),
        vc_latitude: new EntityColumn<string>({ name: 'vc_latitude', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Latitud', defaultValue: '' }),
        vc_longitude: new EntityColumn<string>({ name: 'vc_longitude', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Longitud', defaultValue: '' }),
        vc_floor: new EntityColumn<string>({ name: 'vc_floor', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Piso', defaultValue: '' }),
        vc_apartment: new EntityColumn<string>({ name: 'vc_apartment', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Departamento', defaultValue: '' }),

        vc_utc_offset_minutes: new EntityColumn<string>({ name: 'vc_utc_offset_minutes', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'UTC offset', defaultValue: '' }),


        vc_references: new EntityColumn<string>({ name: 'vc_references', type: 'varchar', length: 255, flags: EntityColumnFlags.None, prompt: 'Referencias', defaultValue: '' }),

        ...DefaultColumns()
    }
)

export class AddressInputTestEntityDef extends EntityDefinition {

    columns = columns();
    views = {};
    lookups = {};

    constructor() {
        super('AddressInputTestEntity');
    }
}

export class AddressInputTestEntity extends Entity<AddressInputTestEntityDef> {

    constructor(client: MicroMClient, parentKeys = {}) {
        super(client, new AddressInputTestEntityDef(), parentKeys);
    }
}