import { Group, Text } from "@mantine/core";
import { IconTrash } from "@tabler/icons-react";
import { EntityClientAction } from "../../Entity";
import { ConfirmAndExecutePanel } from "../../UI";

export const ACTDeleteHistoryDefaultProps = {
    deleteHistoryLabel: 'Delete history',
    deleteHistoryTitle: 'Delete import history',
    deleteHistoryConfirmationLabel: 'Are you sure you want to delete the selected import history records?',
    selectedRecordsLabel: 'Selected records',
    deleteLabel: 'Delete',
};

export const ACTDeleteHistory: EntityClientAction = {
    name: 'ACTDeleteHistory',
    title: <Group spacing="xs"><IconTrash size="1rem" /><Text fw={700}>{ACTDeleteHistoryDefaultProps.deleteHistoryTitle}</Text></Group>,
    label: ACTDeleteHistoryDefaultProps.deleteHistoryLabel,
    icon: <IconTrash size="1rem" />,
    refreshOnClose: true,
    minSelectedRecords: 1,
    showActionInViewMode: false,
    views: ['ipr_brwStandard'],
    onClick: async ({ entity, modal, selectedKeys, element, onClose }) => {
        if (!modal) {
            console.warn('Delete import history action: modal context was not found.');
            return false;
        }

        if (!selectedKeys?.length) {
            console.warn('Delete import history action: no records were selected.');
            return false;
        }

        const abortController = new AbortController();

        await modal.open({
            modalProps: {
                title: <Group spacing="xs"><IconTrash size="1rem" /><Text fw={700}>{ACTDeleteHistoryDefaultProps.deleteHistoryTitle}</Text></Group>,
            },
            focusOnClosed: element,
            content: <ConfirmAndExecutePanel
                content={
                    <Text size="sm" mb="xs">
                        {ACTDeleteHistoryDefaultProps.deleteHistoryConfirmationLabel}{' '}
                        {ACTDeleteHistoryDefaultProps.selectedRecordsLabel}: {selectedKeys.length}.
                    </Text>
                }
                operation="delete"
                okButtonText={ACTDeleteHistoryDefaultProps.deleteLabel}
                onCancel={async () => {
                    abortController.abort();
                    await modal.close();
                }}
                onOK={async () => {
                    const result = await entity.API.deleteData(selectedKeys, abortController.signal, entity.parentKeys);

                    if (!result.Failed) {
                        await modal.close();
                        await onClose?.(true);
                    }

                    return result;
                }}
            />
        });

        return false;
    },
};
