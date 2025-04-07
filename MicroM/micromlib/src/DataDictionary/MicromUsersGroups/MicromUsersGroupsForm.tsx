import { Stack, Tabs, useComponentDefaultProps } from "@mantine/core";
import { IconListCheck, IconUsers, IconUsersGroup } from "@tabler/icons-react";
import { useRef } from "react";
import { DataGridForm, EntityForm, FormOptions, LookupMultiSelect, TextField, useEntityForm, useParentKeys } from "../../UI";
import { MicromUsersGroupsMenus } from "../MicromUsersGroupsMenus";
import { MicromUsersGroups } from "./MicromUsersGroups";

export interface MicromUsersGroupsFormProps extends FormOptions<MicromUsersGroups> {
    groupTabLabel?: string,
    menusTabLabel?: string,
    membersTabLabel?: string,
}

export const MicromUsersGroupsFormDefaultProps: Partial<MicromUsersGroupsFormProps> = {
    initialFormMode: "view",
    groupTabLabel: "Group",
    menusTabLabel: "Menu access",
    membersTabLabel: "Members",
}

const TN = {
    group: 'group', groupIcon: <IconUsersGroup size="1rem" />,
    menus: 'menus', menusIcon: <IconListCheck size="1rem" />,
    members: 'members', membersIcon: <IconUsers size="1rem" />,
};

const gridHeight = "39vh";

export function MicromUsersGroupsForm(props: MicromUsersGroupsFormProps) {

    const {
        entity, initialFormMode, getDataOnInit, onSaved, onCancel, groupTabLabel, menusTabLabel,
        membersTabLabel
    } = useComponentDefaultProps('MicromUsersGroups', MicromUsersGroupsFormDefaultProps, props);

    const formAPI = useEntityForm({ entity: entity, initialFormMode, getDataOnInit: getDataOnInit!, onSaved, onCancel });

    const { formMode, status } = formAPI;

    const groupMenusRef = useRef<MicromUsersGroupsMenus>(new MicromUsersGroupsMenus(entity.API.client));

    const groupMenus = groupMenusRef.current;

    const groupParentkeys = useParentKeys(
        {
            formAPI: formAPI,
            columnNames: [
                entity.def.columns.c_user_group_id.name,
                groupMenus.def.columns.c_menu_id.name
            ],
            entity
        }
    );


    return (
        <EntityForm formAPI={formAPI}>
            <Tabs pt="xs" defaultValue={TN.group} mih="60vh">
                <Tabs.List>
                    <Tabs.Tab value={TN.group} icon={TN.groupIcon} >{groupTabLabel}</Tabs.Tab>
                    <Tabs.Tab value={TN.menus} icon={TN.menusIcon} >{menusTabLabel}</Tabs.Tab>
                    <Tabs.Tab value={TN.members} icon={TN.membersIcon} >{membersTabLabel}</Tabs.Tab>
                </Tabs.List>
                <Tabs.Panel value={TN.group} pt="xs" mih={gridHeight}>
                    <Stack>
                        {formMode != "add" &&
                            <TextField
                                entityForm={formAPI}
                                column={entity.def.columns.c_user_group_id}
                                readOnly={true}
                                required={false}
                                maw="20rem"
                            />
                        }
                        <TextField
                            entityForm={formAPI} column={entity.def.columns.vc_user_group_name} autoFocus transform="capitalize"
                        />
                    </Stack>
                </Tabs.Panel>
                <Tabs.Panel value={TN.menus} pt="xs" mih={gridHeight}>
                    <Stack>
                        <DataGridForm
                            gridHeight={gridHeight}
                            parentKeys={groupParentkeys}
                            key={`grid-menus-${entity.def.name}`}
                            entity={groupMenus}
                            formMode={formMode}
                            viewName={groupMenus.def.views.mmn_brwMenuItems.name}
                            selectionMode="multi"
                            parentFormAPI={formAPI}
                            enableAdd={false}
                            enableEdit={false}
                            enableView={false}
                            enableDelete={false}
                            saveFormBeforeAdd
                            autoSelectFirstRow={false}
                        />
                    </Stack>
                </Tabs.Panel>
                <Tabs.Panel value={TN.members} pt="xs" mih={gridHeight}>
                    <Stack>
                        <LookupMultiSelect
                            entityForm={formAPI}
                            column={entity.def.columns.vc_group_members}
                            entity={entity}
                            lookupDefName={entity.def.lookups.MicromUsers.name}
                            formStatus={status}
                            required={false}
                            searchable={true}
                            includeKeyInDescription={false}
                            creatable={false}
                            maw="35rem"
                        />
                    </Stack>
                </Tabs.Panel>
            </Tabs>
        </EntityForm>
    )
}

