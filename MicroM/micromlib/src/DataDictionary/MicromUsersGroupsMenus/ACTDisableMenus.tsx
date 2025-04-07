import { Group, Text } from "@mantine/core";
import { IconX } from "@tabler/icons-react";
import { MicromUsersGroupsMenus } from ".";
import { EntityClientAction } from "../../Entity";
import { CircleFilledIcon, ConfirmAndExecutePanel } from "../../UI";

export const MicromUsersGroupsMenusDisableMenusLabels = {
    denyAccessToSelectedMenuItems: 'Deny access to selected menu items',
    buttonLabel: 'Deny access',
    title: 'Deny access',
    areYouSure: 'You are denying access to the selected menu items. Are you sure?',
}

const l = MicromUsersGroupsMenusDisableMenusLabels;

export const ACTDisableMenus: EntityClientAction = {
    name: 'ACTDisableMenus',
    label: l.buttonLabel,
    title: <Group spacing="xs"><CircleFilledIcon icon={<IconX size="0.75rem" />} backColor="red" /><Text fw={700}>{l.title}</Text></Group>,
    refreshOnClose: true,
    dontRequireSelection: true,
    onClick: async ({ entity, modal, selectedKeys, element, onClose }) => {
        // cast entity
        const parent = entity as MicromUsersGroupsMenus;
        const abort_controller = new AbortController();

        if (modal) {
            await modal.open({
                modalProps: {
                    title: <Group spacing="xs"><CircleFilledIcon icon={<IconX size="0.75rem" />} backColor="red" /><Text fw={700}>{l.denyAccessToSelectedMenuItems}</Text></Group>,
                },
                focusOnClosed: element,
                content: <ConfirmAndExecutePanel
                    content={<Text size="sm" mb="xs">{l.areYouSure}</Text>}
                    operation="proc"
                    onCancel={async () => {
                        abort_controller.abort();
                        await modal.close();
                        if (onClose) await onClose(false);
                    }}
                    onOK={
                        async () => {
                            const result = await parent.API.deleteData(selectedKeys, abort_controller.signal, parent.parentKeys);
                            const success = result.Failed === false;
                            if (success) {
                                await modal.close();
                                if (onClose) await onClose(success);
                            }
                            return result;
                        }
                    }
                />

            });
        }
        else {
            console.log('modal context not found');
        }

        return Promise.resolve(false);

    }

}

