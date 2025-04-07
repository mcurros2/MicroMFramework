import { DataGridToolbarSizes } from "./DataGridToolbar";

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