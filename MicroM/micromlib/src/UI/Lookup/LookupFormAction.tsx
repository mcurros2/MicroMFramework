import { Button, Group, Stack, Text, useComponentDefaultProps } from "@mantine/core";
import { IconAlertCircle, IconBoxMultiple } from "@tabler/icons-react";
import { Entity, EntityDefinition } from "../../Entity";
import { DBStatusResult } from "../../client";
import { ConfirmAndExecutePanel, FakeProgressBar, useModal } from "../Core";
import { DataGridDefaultProps, DataGridSelectionKeys } from "../DataGrid";
import { LookupForm } from "./LookupForm";


export interface LookupFormActionProps {
    lookupEntity: Entity<EntityDefinition>,
    viewName: string,
    title: string,
    onOK: (keys: DataGridSelectionKeys, abortSignal: AbortSignal) => Promise<DBStatusResult>,
    onCancel?: () => void,
    onActionFinished?: () => Promise<void>,
    addingRecords?: string
    runOnOpen?: boolean
    confirmContent?: React.ReactNode,
    showActions?: boolean
}

export const LookupFormActionDefaultProps: Partial<LookupFormActionProps> = {
    addingRecords: "Adding records...",
    confirmContent: "Do you wish to add the selected records?",
    showActions: false
};


export function LookupFormAction(props: LookupFormActionProps) {
    const {
        lookupEntity, viewName, onOK, addingRecords, title, onCancel, onActionFinished, runOnOpen,
        confirmContent, showActions
    } = useComponentDefaultProps('LookupFormAction', LookupFormActionDefaultProps, props);

    const modal = useModal();

    const LookpIcon = lookupEntity.Icon ?? IconBoxMultiple;

    const labels = DataGridDefaultProps.labels;

    return (
        <LookupForm
            dataGridProps={
                {
                    entity: lookupEntity,
                    viewName: lookupEntity.def.views[viewName].name,
                    limit: "10000",
                    refreshOnInit: true,
                    selectionMode: 'multi',
                    enableAdd: false,
                    enableEdit: false,
                    enableDelete: false,
                    parentKeys: lookupEntity.parentKeys,
                    showActions: showActions
                }
            }
            onOK={
                async (keys) => {
                    const abort_controller = new AbortController();

                    if (keys.length > 0) {
                        await modal?.open({
                            modalProps: {
                                title: <Group><LookpIcon size="1.5rem" stroke="1.5"></LookpIcon> <Text fw="700">{title}</Text></Group>,
                            },
                            content: <ConfirmAndExecutePanel
                                loadingContent={
                                    <Stack>
                                        <FakeProgressBar />
                                        <Text size="sm" mb="xs">{addingRecords}</Text>
                                    </Stack>
                                }
                                onOK={
                                    async () => {
                                        const result = await onOK(keys, abort_controller.signal);

                                        if (!result.Failed) {
                                            // close this modal
                                            await modal.close();
                                            // close selection modal
                                            await modal.close();
                                            if (onActionFinished) await onActionFinished();
                                        }
                                        return result;
                                    }
                                }
                                onCancel={async () => {
                                    await abort_controller.abort();
                                    // close this modal
                                    await modal.close();
                                    // close selection modal
                                    await modal.close();
                                    if (onActionFinished) await onActionFinished();
                                }}
                                content={confirmContent}
                                runOnOpen={runOnOpen}
                                operation="add"
                            />

                        });
                    }
                    else {
                        modal.open({
                            modalProps: {
                                title: <Group><IconAlertCircle size="1.5rem" stroke="1.5" /> <Text fw="700">{labels?.warningLabel}</Text></Group>,
                            },
                            content:
                                <>
                                    <Text size="sm" mb="xs">
                                        {labels?.YouMustSelectOneOrMoreRecordsToExecuteAction}
                                    </Text>
                                    <Group mt="xs" position="right">
                                        <Button onClick={async () => await modal.close()}>{labels?.closeLabel}</Button>
                                    </Group>
                                </>
                        });
                    }
                }
            }
            onCancel={async () => {
                await modal.close();
                if (onCancel) onCancel();
            }}
        />
    );
}