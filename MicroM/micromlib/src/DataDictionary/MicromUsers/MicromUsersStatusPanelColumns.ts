import { EntityColumn, EntityColumnFlags, CommonFlags as c } from "../../Entity"
import { MicroMClient, ValuesObject } from "../../client";
import { MicromUsersGroups } from "../MicromUsersGroups";

export interface IMicromUserStatusPanelColums {
    vc_username: EntityColumn<string>,
    vc_password: EntityColumn<string>,
    bt_islocked: EntityColumn<boolean>,
    bt_disabled: EntityColumn<boolean>,
    i_badlogonattempts: EntityColumn<number>,
    i_locked_minutes_remaining: EntityColumn<number>,
    dt_locked: EntityColumn<Date>,
    dt_last_login: EntityColumn<Date>,
    dt_last_refresh: EntityColumn<Date>,
    vc_user_groups: EntityColumn<string[]>,
}

export const MicromUserStatusPanelColumsKeys: string[] = [
    'vc_username',
    'vc_password',
    'bt_islocked',
    'bt_disabled',
    'i_bad_logon_attempts',
    'i_locked_minutes_remaining',
    'dt_locked',
    'dt_last_login',
    'dt_last_refresh',
    'vc_user_groups',
]

export const MicromUserStatusPanelColumns = (default_groups?: string[]): IMicromUserStatusPanelColums => (
    {
        vc_username: new EntityColumn<string>({ name: 'vc_username', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.fake, prompt: 'Username' }),
        vc_password: new EntityColumn<string>({ name: 'vc_password', type: 'varchar', length: 255, flags: c.Edit | EntityColumnFlags.fake | EntityColumnFlags.nullable, prompt: 'Password' }),
        bt_disabled: new EntityColumn<boolean>({ name: 'bt_disabled', type: 'bit', length: 1, flags: c.Edit | EntityColumnFlags.fake, prompt: 'Disable user' }),
        bt_islocked: new EntityColumn<boolean>({ name: 'bt_islocked', type: 'bit', length: 1, flags: EntityColumnFlags.fake, prompt: 'Locked' }),
        i_badlogonattempts: new EntityColumn<number>({ name: 'i_badlogonattempts', type: 'int', flags: EntityColumnFlags.fake, prompt: 'Badlogonattempts' }),
        i_locked_minutes_remaining: new EntityColumn<number>({ name: 'i_locked_minutes_remaining', type: 'int', flags: EntityColumnFlags.fake, prompt: 'Locked minutes remaining' }),
        dt_locked: new EntityColumn<Date>({ name: 'dt_locked', type: 'datetime', flags: EntityColumnFlags.fake, prompt: 'Date Locked' }),
        dt_last_login: new EntityColumn<Date>({ name: 'dt_last_login', type: 'datetime', flags: EntityColumnFlags.fake, prompt: 'Last Login', description: 'The user last login date' }),
        dt_last_refresh: new EntityColumn<Date>({ name: 'dt_last_refresh', type: 'datetime', flags: EntityColumnFlags.fake, prompt: 'Last Refresh', description: 'The user last session refresh' }),
        vc_user_groups: new EntityColumn<string[]>({ name: 'vc_user_groups', type: 'varchar', flags: c.Edit | EntityColumnFlags.fake | EntityColumnFlags.nullable, prompt: 'User Groups', isArray: true, defaultValue: default_groups }),
    }
)

export const MicromUsersStatusPanelLookups = () => (
    {
        'MicromUsersGroups': {
            name: 'MicromUsersGroups',
            viewMapping: { keyIndex: 0, descriptionIndex: 1 },
            entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new MicromUsersGroups(client, parentKeys)
        }
    }
);