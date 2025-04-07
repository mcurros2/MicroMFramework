import { DefaultColumns, EntityColumn, EntityColumnFlags, EntityDefinition, CommonFlags as c } from "../../Entity";
import { MicroMClient, ValuesObject } from "../../client";
import { catUserTypes } from "../Config/catUserTypes";
import { MicromUsersGroups } from "../MicromUsersGroups";

const columns = () =>
(
    {
        c_user_id: new EntityColumn<string>({ name: 'c_user_id', type: 'char', length: 20, flags: c.PKAutonum, prompt: 'User Id' }),
        vc_username: new EntityColumn<string>({ name: 'vc_username', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Username' }),
        vc_email: new EntityColumn<string>({ name: 'vc_email', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Email' }),
        vc_pwhash: new EntityColumn<string>({ name: 'vc_pwhash', type: 'varchar', length: 2048, flags: c.Edit, prompt: 'Pwhash' }),
        vb_sid: new EntityColumn<string>({ name: 'vb_sid', type: 'varchar', length: 85, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Sid' }),
        vc_refreshtoken: new EntityColumn<string>({ name: 'vc_refreshtoken', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Refreshtoken' }),
        dt_refresh_expiration: new EntityColumn<boolean>({ name: 'dt_refresh_expiration', type: 'datetime', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Refresh Expiration' }),
        i_badlogonattempts: new EntityColumn<number>({ name: 'i_badlogonattempts', type: 'int', flags: c.Edit, prompt: 'Badlogonattempts' }),
        i_refreshcount: new EntityColumn<number>({ name: 'i_refreshcount', type: 'int', flags: c.Edit, prompt: 'Refreshcount' }),
        bt_disabled: new EntityColumn<boolean>({ name: 'bt_disabled', type: 'bit', length: 1, flags: c.Edit, prompt: 'Disable user' }),
        dt_locked: new EntityColumn<Date>({ name: 'dt_locked', type: 'datetime', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Date Locked' }),
        dt_last_login: new EntityColumn<Date>({ name: 'dt_last_login', type: 'datetime', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Last Login' }),
        dt_last_refresh: new EntityColumn<Date>({ name: 'dt_last_refresh', type: 'datetime', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Last Refresh' }),
        vc_recovery_code: new EntityColumn<string>({ name: 'vc_recovery_code', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Recovery Code' }),
        dt_last_recovery: new EntityColumn<Date>({ name: 'dt_last_recovery', type: 'datetime', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'Last Recovery' }),
        c_usertype_id: new EntityColumn<string>({ name: 'c_usertype_id', type: 'char', length: 20, flags: c.Edit, prompt: 'Usertype Id' }),
        vc_user_groups: new EntityColumn<string[]>({ name: 'vc_user_groups', type: 'varchar', flags: c.Edit | EntityColumnFlags.nullable, prompt: 'User Groups', isArray: true }),
        bt_islocked: new EntityColumn<boolean>({ name: 'bt_islocked', type: 'bit', length: 1, flags: EntityColumnFlags.fake, prompt: 'Locked' }),
        i_locked_minutes_remaining: new EntityColumn<number>({ name: 'i_locked_minutes_remaining', type: 'int', flags: EntityColumnFlags.fake, prompt: 'Locked minutes remaining' }),
        vc_password: new EntityColumn<string>({ name: 'vc_password', type: 'varchar', length: 255, flags: c.Edit, prompt: 'Password' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        usr_brwStandard: { name: 'usr_brwStandard', keyMappings: { c_user_id: 0 } }
    }
)


const lookups = () =>
(
    {
        'UserTypes': {
            name: 'UserTypes',
            viewMapping: { keyIndex: 0, descriptionIndex: 1 },
            entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new catUserTypes(client, parentKeys)
        },
        'MicromUsersGroups': {
            name: 'MicromUsersGroups',
            viewMapping: { keyIndex: 0, descriptionIndex: 1 },
            entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new MicromUsersGroups(client, parentKeys)
        }
    }
)

export class MicromUsersDef extends EntityDefinition {

    columns = columns();
    views = views();
    lookups = lookups();

    constructor() {
        super('MicromUsers');
    }
}