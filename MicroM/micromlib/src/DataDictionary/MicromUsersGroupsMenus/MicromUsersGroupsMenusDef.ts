import { DefaultColumns, EntityColumn, EntityDefinition, EntityView, CommonFlags as c } from "../../Entity";
import { MicroMClient, ValuesObject } from "../../client";
import { MicromUsersGroups } from "../MicromUsersGroups";
import { ACTDisableMenus } from "./ACTDisableMenus";
import { ACTEnableMenus } from "./ACTEnableMenus";
import { MicromUsersGroupsMenusColumnsOverrides } from "./MicromUsersGroupsMenusColumnsOverrides";


const columns = () =>
(
    {
        c_user_group_id: new EntityColumn<string>({ name: 'c_user_group_id', type: 'char', length: 20, flags: c.PK, prompt: 'User Group Id' }),
        c_menu_id: new EntityColumn<string>({ name: 'c_menu_id', type: 'char', length: 20, flags: c.PK, prompt: 'Menu Id' }),
        c_menu_item_id: new EntityColumn<string>({ name: 'c_menu_item_id', type: 'char', length: 20, flags: c.PK, prompt: 'Menu Item Id' }),
        ...DefaultColumns()
    }
)

const views = () =>
(
    {
        mmn_brwStandard: { name: 'mmn_brwStandard', keyMappings: { c_user_group_id: 0 } },
        mmn_brwMenuItems: { name: 'mmn_brwMenuItems', keyMappings: { c_menu_id: 0, c_menu_item_id: 1 }, gridColumnsOverrides: MicromUsersGroupsMenusColumnsOverrides },
    } as Record<string, EntityView>
)

const lookups = () =>
(
    {
        'MicromUsersGroups': {
            name: 'MicromUsersGroups',
            viewMapping: { keyIndex: 0, descriptionIndex: 1 },
            entityConstructor: (client: MicroMClient, parentKeys?: ValuesObject) => new MicromUsersGroups(client, parentKeys)
        },

    }
)

const clientActions = () =>
(
    {
        ACTEnableMenus: ACTEnableMenus,
        ACTDisableMenus: ACTDisableMenus,
    }
)

export class MicromUsersGroupsMenusDef extends EntityDefinition {

    columns = columns();
    views = views();
    lookups = lookups();
    clientActions = clientActions();

    constructor() {
        super('MicromUsersGroupsMenus');
    }
}