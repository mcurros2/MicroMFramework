import { Box, Button, Group, useComponentDefaultProps } from "@mantine/core";
import { EntityClientAction } from "../../Entity";
import { useRef } from "react";
import { getToolbarSizes } from "./ToolBarFunctions";
import { DataGridToolbarSizes } from "./DataGridToolbar";
import { ButtonVariant, FormMode } from "../Core";

export interface DataGridActionsToolbarProps {
    size?: DataGridToolbarSizes,
    viewName?: string,

    addLabel: string,
    editLabel: string,
    deleteLabel: string,
    viewLabel: string,

    enableAdd?: boolean,
    enableEdit?: boolean,
    enableDelete?: boolean,
    enableView?: boolean,

    showActions?: boolean,
    clientActions: Record<string, EntityClientAction>,
    actionsButtonVariant?: ButtonVariant,
    handleExecuteAction: (action: EntityClientAction, element?: HTMLElement) => void,

    onAddClick?: (element: HTMLElement) => void,
    onEditClick?: (element: HTMLElement) => void,
    onDeleteClick?: (element: HTMLElement) => void,
    onViewClick?: (element: HTMLElement) => void,

    parentFormMode?: FormMode

}

export const DataGridActionsToolbarDefaultProps: Partial<DataGridActionsToolbarProps> = {
    size: "sm",
    actionsButtonVariant: "light",
    showActions: true,
}

export function DataGridActionsToolbar(props: DataGridActionsToolbarProps) {
    const {
        size, viewName,
        addLabel, editLabel, deleteLabel, viewLabel,
        enableAdd, enableEdit, enableDelete, enableView,
        showActions, clientActions, actionsButtonVariant, handleExecuteAction,
        onAddClick, onEditClick, onDeleteClick, onViewClick, parentFormMode
    } = useComponentDefaultProps('DataGridActionsToolbar', DataGridActionsToolbarDefaultProps, props);

    const addElement = useRef<HTMLButtonElement>(null);
    const editElement = useRef<HTMLButtonElement>(null);
    const deleteElement = useRef<HTMLButtonElement>(null);
    const viewElement = useRef<HTMLButtonElement>(null);

    const actionElements = useRef<(HTMLButtonElement | null)[]>([]);

    const { buttonsSize} = getToolbarSizes(size!);


    return (
        <Box>
            <Group>
                {enableAdd && onAddClick && <Button size={buttonsSize} ref={addElement} variant={actionsButtonVariant} onClick={async () => await onAddClick(addElement.current as HTMLElement)}>{addLabel}</Button>}
                {enableEdit && onEditClick && <Button size={buttonsSize} ref={editElement} variant={actionsButtonVariant} onClick={async () => await onEditClick(editElement.current as HTMLElement)}>{editLabel}</Button>}
                {enableDelete && onDeleteClick && <Button size={buttonsSize} ref={deleteElement} variant={actionsButtonVariant} onClick={async () => await onDeleteClick(deleteElement.current as HTMLElement)}>{deleteLabel}</Button>}
                {enableView && onViewClick && <Button size={buttonsSize} ref={viewElement} variant={actionsButtonVariant} onClick={async () => await onViewClick(viewElement.current as HTMLElement)}>{viewLabel}</Button>}
                {showActions &&
                    Object.values(clientActions).map(
                        (action, index) => {
                            if (parentFormMode === undefined) return null;
                            if (parentFormMode === 'view' && !action.showActionInViewMode) return null;
                            if (viewName && action.views && !action.views.includes(viewName)) return null;
                            return (
                                <Button leftIcon={action.icon} key={action.name} ref={(el) => (actionElements.current[index] = el)} size={buttonsSize} variant={actionsButtonVariant} onClick={() => handleExecuteAction(action, actionElements.current[index] ?? undefined)}>{action.label}</Button>
                            )
                        }
                    )
                }
            </Group>
        </Box>
    );
}