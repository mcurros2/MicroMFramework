import { MantineTheme } from "@mantine/core";
import { SpotlightAction } from "@mantine/spotlight";
import { Dispatch, ReactNode, SetStateAction, useMemo, useState } from "react";
import { MicroMClient, MicroMClientClaimTypes } from "../../client";
import { navigateToRoute } from "../Router/MicroMRouterState";
import { getMenuItemContext, MenuItem } from "./MenuItem";

export interface MenuContentProps {
    client: MicroMClient,
    setContent: Dispatch<SetStateAction<ReactNode>>,
    setOpened: Dispatch<SetStateAction<boolean>>,
    isLoggedIn: boolean | undefined,
    setIsLoggedIn: Dispatch<SetStateAction<boolean | undefined>>,
    loggedInInfo?: Partial<MicroMClientClaimTypes>,
    theme?: MantineTheme
}

export interface MenuConfigItem {
    menu: (props: MenuContentProps) => MenuItem[],
    isMain?: boolean
}

export interface UseMenuContentProps extends MenuContentProps {
    menus: Record<string, MenuConfigItem>,
    enableMenuSecurity?: boolean,
    searchMenuItemsWithContentOnly?: boolean
}

export interface MenuItemActionProps {
    item: MenuItem,
    setContent: Dispatch<SetStateAction<ReactNode>>,
    setOpened: Dispatch<SetStateAction<boolean>>,
    clearContent: boolean,
    defaultLoadingComponent?: ReactNode,
    onActiveChange: Dispatch<SetStateAction<string>>,
    onSubitemActiveChange: Dispatch<SetStateAction<string>>,
    autoHideNavBarOnClick?: boolean
}

export interface MenuContentResult {
    activeIDState: [string, Dispatch<SetStateAction<string>>],
    subitemActiveIDState: [string, Dispatch<SetStateAction<string>>],
    items: MenuItem[],
    actions: SpotlightAction[],
    menuPathsDictionary: Record<string, MenuItem>
}

export interface UseMenuContentResult {
    menus: Record<string, MenuContentResult>,
    mainMenuId: string,
    mainMenu: MenuContentResult
}

const triggerItemAction = async (props: MenuItemActionProps) => {
    const { item, setOpened, onActiveChange, onSubitemActiveChange, autoHideNavBarOnClick } = props;
    if (!item.noActive) {
        onActiveChange(item.ID);
        onSubitemActiveChange(item.ID);
    }
    else {
        onActiveChange('');
        onSubitemActiveChange('');
    }

    if (item.onClick) {
        await item.onClick(getMenuItemContext(item));
    }

    if ((item.content || item.subitems) && item.menuPath) {
        navigateToRoute(item.menuPath);
    }

    if (autoHideNavBarOnClick && !item.subitems) setOpened(false);
}

const populateMenuPaths = (items: MenuItem[], parentPath: string, parentDescription: string, menuId: string) => {
    items.forEach(item => {
        item.menuId = menuId;

        const currentPath = `${parentPath}/${item.ID}`;
        item.menuPath = currentPath;

        const currentDescription = parentDescription
            ? `${parentDescription} / ${item.label}`
            : item.label;

        item.menuPathDescription = currentDescription;

        if (item.subitems && item.subitems.length > 0) {
            populateMenuPaths(item.subitems, currentPath, currentDescription, menuId);
        }
    });
};

const createMenuDictionary = (items: MenuItem[], dictionary: Record<string, MenuItem> = {}) => {
    for (const item of items) {
        if (item.menuPath) {
            dictionary[item.menuPath] = item;

            if (item.subitems && item.subitems.length > 0) {
                createMenuDictionary(item.subitems, dictionary);
            }
        }
    }
    return dictionary;
};

const filterEnabledItems = (items: MenuItem[], menuId: string, enabled: Set<string>): MenuItem[] => {
    return items
        .filter(item => enabled.has(`${menuId}_${item.ID}`))
        .map(item => {
            if (item.subitems) {
                const filteredSubitems = filterEnabledItems(item.subitems, menuId, enabled);
                return {
                    ...item,
                    subitems: filteredSubitems.length > 0 ? filteredSubitems : undefined
                };
            }
            return item;
        });
};

const createSpotlightActions = (items: MenuItem[], baseActionProps: Omit<MenuItemActionProps, 'item' | 'autoHideNavBarOnClick'>, searchMenuItemsWithContentOnly: boolean, actions: SpotlightAction[] = []): SpotlightAction[] => {
    for (const item of items) {
        if (item.section === 'items') {
            const group = item.menuPathDescription
                ?.split("/")
                .map(part => part.trim())
                .filter(Boolean)[0];

            if (!searchMenuItemsWithContentOnly || item.content || item.onClick) {
                actions.push({
                    id: item.ID,
                    title: item.label,
                    description: `${item.menuPathDescription}${item.description ? ` - ${item.description}` : ""}`,
                    group,
                    icon: item.icon,
                    onTrigger: () => triggerItemAction({ ...baseActionProps, item }),
                });
            }
            if (item.subitems) {
                createSpotlightActions(item.subitems, baseActionProps, searchMenuItemsWithContentOnly, actions);
            }
        }
    }
    return actions;
};

const createMenuStateSetters = (menuIds: string[], setState: Dispatch<SetStateAction<Record<string, string>>>) => Object.fromEntries(menuIds.map(menuId => [
    menuId,
    ((value: SetStateAction<string>) => {
        setState(current => {
            const currentValue = current[menuId] ?? '';
            const nextValue = typeof value === 'function' ? value(currentValue) : value;
            return nextValue === currentValue ? current : { ...current, [menuId]: nextValue };
        });
    }) as Dispatch<SetStateAction<string>>
]));

export function useMenuContent(props: UseMenuContentProps): UseMenuContentResult {
    const {
        menus: menuConfig, client, setContent, setOpened, isLoggedIn, setIsLoggedIn, loggedInInfo, theme, enableMenuSecurity = true, searchMenuItemsWithContentOnly = true
    } = props;

    const menuIds = useMemo(() => Object.keys(menuConfig), [menuConfig]);

    const mainMenuId = useMemo(() => {
        const mainMenuIds = Object.entries(menuConfig)
            .filter(([, config]) => config.isMain)
            .map(([menuId]) => menuId);

        if (mainMenuIds.length !== 1) {
            throw new Error(`useMenuContent requires exactly one main menu; received ${mainMenuIds.length}.`);
        }

        return mainMenuIds[0];
    }, [menuConfig]);

    const [activeIds, setActiveIds] = useState<Record<string, string>>({});
    const [subitemActiveIds, setSubitemActiveIds] = useState<Record<string, string>>({});

    const activeSetters = useMemo(
        () => createMenuStateSetters(menuIds, setActiveIds),
        [menuIds]
    );
    const subitemActiveSetters = useMemo(
        () => createMenuStateSetters(menuIds, setSubitemActiveIds),
        [menuIds]
    );

    const materializedMenus = useMemo(() => {
        const enabledMenus = client.getMenus();
        const menuProps: MenuContentProps = {
            client,
            setContent,
            setOpened,
            isLoggedIn,
            setIsLoggedIn,
            loggedInInfo,
            theme
        };

        return Object.fromEntries(Object.entries(menuConfig).map(([menuId, config]) => {
            const allItems = config.menu(menuProps);
            populateMenuPaths(allItems, `/${menuId}`, '', menuId);

            const items = isLoggedIn
                ? enableMenuSecurity
                    ? filterEnabledItems(allItems, menuId, enabledMenus)
                    : allItems
                : [];

            return [menuId, {
                items,
                menuPathsDictionary: createMenuDictionary(items)
            }];
        }));
    }, [menuConfig, client, setContent, setOpened, isLoggedIn, setIsLoggedIn, loggedInInfo, theme, enableMenuSecurity]);

    const menus = useMemo(() => Object.fromEntries(menuIds.map(menuId => {
        const materializedMenu = materializedMenus[menuId];
        const baseActionProps = {
            setContent,
            setOpened,
            clearContent: true,
            onActiveChange: activeSetters[menuId],
            onSubitemActiveChange: subitemActiveSetters[menuId]
        };

        const result: MenuContentResult = {
            activeIDState: [activeIds[menuId] ?? '', activeSetters[menuId]],
            subitemActiveIDState: [subitemActiveIds[menuId] ?? '', subitemActiveSetters[menuId]],
            items: materializedMenu.items,
            actions: createSpotlightActions(materializedMenu.items, baseActionProps, searchMenuItemsWithContentOnly),
            menuPathsDictionary: materializedMenu.menuPathsDictionary
        };

        return [menuId, result];
    })), [menuIds, materializedMenus, setContent, setOpened, activeIds, activeSetters, subitemActiveIds, subitemActiveSetters, searchMenuItemsWithContentOnly]);

    return useMemo(() => ({
        menus,
        mainMenuId,
        mainMenu: menus[mainMenuId]
    }), [menus, mainMenuId]);
}

export type UseMenuContentReturnType = UseMenuContentResult;
