import { EntityColumn, DefaultColumns, EntityDefinition, CommonFlags as c } from "../../Entity";
import { MicroMClient, ValuesObject } from "../../client";
import { MicromUsers } from "../MicromUsers/MicromUsers";
import { MicromUsersGroups } from "../MicromUsersGroups/MicromUsersGroups";

const columns = () =>
(
    {
        c_user_group_id: new EntityColumn<string>({ name: 'c_user_group_id', type: 'char', length: 20, flags: c.PK, prompt: 'User Group Id' }),
        c_user_id: new EntityColumn<string>({ name: 'c_user_id', type: 'char', length: 20, flags: c.PK, prompt: 'User Id' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        mgm_brwStandard: { name: 'mgm_brwStandard', keyMappings: { c_user_id: 0 } }
    }
)

const lookups = () =>
(
    {
        'MicromUsers': {
            name: 'MicromUsers',
            viewMapping: { keyIndex: 0, descriptionIndex: 1 },
            entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new MicromUsers(client, parentKeys)
        },

        'MicromUsersGroups': {
            name: 'MicromUsersGroups',
            viewMapping: { keyIndex: 0, descriptionIndex: 1 },
            entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new MicromUsersGroups(client, parentKeys)
        },

    }
)

export class MicromUsersGroupsMembersDef extends EntityDefinition {

    columns = columns();
    views = views();
    lookups = lookups();

    constructor() {
        super('MicromUsersGroupsMembers');
    }
}