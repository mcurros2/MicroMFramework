import { MenuConfigItem } from "./useMenuContent";

export function getMainMenuId(menuConfig: Record<string, MenuConfigItem>): string {
    const mainMenuIds = Object.keys(menuConfig).filter(menuId => menuConfig[menuId].isMain);

    if (mainMenuIds.length !== 1) {
        throw new Error(`getMainMenuId: exactly one main menu (isMain: true) is required; found ${mainMenuIds.length}.`);
    }

    return mainMenuIds[0];
}
