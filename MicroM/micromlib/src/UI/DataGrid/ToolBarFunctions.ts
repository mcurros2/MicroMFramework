import type { EntityClientAction } from "../../Entity";
import type { EntityUILabels } from "../Core";
import type { DataGridToolbarSizes } from "./DataGridToolbar";

export interface EntityActionNames {
    addActionName?: string,
    editActionName?: string,
    deleteActionName?: string,
    viewActionName?: string,
}

function getActionLabel(clientActions: Record<string, EntityClientAction>, actionName: string | undefined, fallbackLabel: string) {
    const actionLabel = actionName ? clientActions[actionName]?.label : undefined;
    return typeof actionLabel === 'string' ? actionLabel : fallbackLabel;
}

export function getOverriddenActionLabels<T extends EntityUILabels>(labels: T, clientActions: Record<string, EntityClientAction>, actionNames: EntityActionNames): T {
    return {
        ...labels,
        addLabel: getActionLabel(clientActions, actionNames.addActionName, labels.addLabel),
        editLabel: getActionLabel(clientActions, actionNames.editActionName, labels.editLabel),
        deleteLabel: getActionLabel(clientActions, actionNames.deleteActionName, labels.deleteLabel),
        viewLabel: getActionLabel(clientActions, actionNames.viewActionName, labels.viewLabel),
    };
}

export function getToolbarSizes(size: DataGridToolbarSizes) {

    let buttonsSize: string | undefined;
    let iconsSize = '';
    let actionIconSize;
    let badgeSize;
    switch (size) {
        case "xs":
            iconsSize = "1rem";
            buttonsSize = "xs";
            actionIconSize = "sm";
            badgeSize = "sm";
            break;
        case "sm":
            iconsSize = "1.1rem";
            buttonsSize = "xs";
            actionIconSize = "md";
            badgeSize = "lg";
            break;
        case "md":
            iconsSize = "1.1rem";
            buttonsSize = "sm";
            actionIconSize = "md";
            badgeSize = "xl";
            break;
        case "lg":
            iconsSize = "1.5rem";
            buttonsSize = "md";
            actionIconSize = "md";
            badgeSize = "xl";
            break;
        case "xl":
            iconsSize = "1.7rem";
            buttonsSize = "lg";
            actionIconSize = "md";
            badgeSize = "xl";
            break;
        default:
            iconsSize = "1.1rem";
            buttonsSize = "sm";
            actionIconSize = "md";
            badgeSize = "lg";
            break;
    }

    return { buttonsSize, iconsSize, actionIconSize, badgeSize };
}
