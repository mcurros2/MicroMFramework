import { Text } from "@mantine/core";
import { ModalSettings } from '@mantine/modals/lib/context';
import { Entity, EntityDefinition } from "../../Entity";
import { FormMode, useModal } from "../Core";
import { DataGridForm } from "../DataGrid";
import { GridSelectionMode } from "../Grid";

export interface useDataGridFormType {
    open: ({ entity, viewName, onClosed, selectionMode, modalProps, search, modalTitle, gridTitle }: ModalDataGridProps) => void;
}

export interface ModalDataGridProps {
    entity: Entity<EntityDefinition>,
    viewName: string,
    onClosed?: () => void,
    modalProps?: ModalSettings,
    selectionMode?: GridSelectionMode,
    search?: string[] | undefined,
    modalTitle?: string
    gridTitle?: React.ReactNode,
    formMode: FormMode
}

const defaultProps: Partial<ModalDataGridProps> = {
    modalProps: { size: 'xl' }
}

export function useDataGridForm() {
    const modals = useModal();

    const open = async (props: ModalDataGridProps) => {
        const { entity, viewName, onClosed, modalProps, selectionMode, search, modalTitle, gridTitle, formMode } = { ...defaultProps, ...props };

        const handleClosed = async () => {
            if (onClosed) {
                onClosed();
            }
        }

        await modals.open(
            {
                content: <DataGridForm
                    entity={entity}
                    viewName={viewName}
                    formMode={formMode}
                    refreshOnInit
                    selectionMode={selectionMode ?? 'single'}
                    customTitle={gridTitle}
                    search={search}
                />,
                modalProps: {
                    ...modalProps,
                    trapFocus: true,
                    returnFocus: true,
                    title: <Text fw="700">{modalTitle}</Text>,
                },
                onClosed: async () => {
                    await handleClosed();
                }

            });

    };

    return open;

}