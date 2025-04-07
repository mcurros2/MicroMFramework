import { Group, Text } from "@mantine/core";
import { IconCheck } from "@tabler/icons-react";
import { MicromUsersGroupsMenus } from ".";
import { EntityClientAction } from "../../Entity";
import { CircleFilledIcon, ConfirmAndExecutePanel } from "../../UI";

export const MicromUsersGroupsMenusEnableMenusLabels = {
    allowAccessToSelectedMenuItems: 'Allow access to selected menu items',
    buttonLabel: 'Allow access',
    title: 'Enable access',
    areYouSure: 'You are allowing access to the selected menu items. Are you sure?',
}

export const ACTEnableMenus: EntityClientAction = {
    name: 'ACTEnableMenus',
    label: MicromUsersGroupsMenusEnableMenusLabels.buttonLabel,
    title: <Group spacing="xs"><CircleFilledIcon icon={<IconCheck size="0.75rem" />} backColor="green" /><Text fw={700}>{MicromUsersGroupsMenusEnableMenusLabels.title}</Text></Group>,
    refreshOnClose: true,
    dontRequireSelection: true,
    onClick: async ({ entity, modal, selectedKeys, element, onClose }) => {
        // cast entity
        const parent = entity as MicromUsersGroupsMenus;
        const abort_controller = new AbortController();

        if (modal) {
            await modal.open({
                modalProps: {
                    title: <Group spacing="xs"><CircleFilledIcon icon={<IconCheck size="0.75rem" />} backColor="green" /><Text fw={700}>{MicromUsersGroupsMenusEnableMenusLabels.allowAccessToSelectedMenuItems}</Text></Group>,
                },
                focusOnClosed: element,
                content: <ConfirmAndExecutePanel
                    content={<Text size="sm" mb="xs">{MicromUsersGroupsMenusEnableMenusLabels.areYouSure}</Text>}
                    operation="proc"
                    onCancel={async () => {
                        abort_controller.abort();
                        await modal.close();
                        if (onClose) await onClose(false);
                    }}
                    onOK={
                        async () => {
                            const result = await parent.API.addData(abort_controller.signal, selectedKeys, true);
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

