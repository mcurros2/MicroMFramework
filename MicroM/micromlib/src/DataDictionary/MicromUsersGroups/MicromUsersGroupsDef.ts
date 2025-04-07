import { DefaultColumns, EntityColumn, EntityColumnFlags, EntityDefinition, CommonFlags as c } from "../../Entity";
import { MicroMClient, ValuesObject } from "../../client";
import { MicromUsers } from "../MicromUsers";

const columns = () =>
(
    {
        c_user_group_id: new EntityColumn<string>({ name: 'c_user_group_id', type: 'char', length: 20, flags: c.PKAutonum, prompt: 'User Group Id' }),
        vc_user_group_name: new EntityColumn<string>({
            name: 'vc_user_group_name', type: 'varchar', length: 255, flags: c.Edit, prompt: 'User Group Name'
            , description: 'The name of the user group'
            
        }),
        vc_group_members: new EntityColumn<string[]>({
            name: 'vc_group_members', type: 'varchar', flags: c.Edit | EntityColumnFlags.fake | EntityColumnFlags.nullable,
            prompt: 'Members',
            description: 'The users members of the user group',
            isArray: true
        }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        mug_brwStandard: { name: 'mug_brwStandard', keyMappings: { c_user_group_id: 0 } }
    }
)

const lookups = () => (
    {
        'MicromUsers': {
            name: 'MicromUsers',
            viewMapping: { keyIndex: 0, descriptionIndex: 1 },
            entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new MicromUsers(client, parentKeys)
        }
    }
);

export class MicromUsersGroupsDef extends EntityDefinition {

    columns = columns();
    views = views();
    lookups = lookups();

    constructor() {
        super('MicromUsersGroups');
    }
}