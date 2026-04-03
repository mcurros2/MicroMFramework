import { MantineTheme, Skeleton, useComponentDefaultProps } from "@mantine/core";
import { SpotlightAction } from "@mantine/spotlight";
import { Dispatch, ReactNode, SetStateAction, useEffect, useMemo, useState } from "react";
import { MicroMClient, MicroMClientClaimTypes } from "../../client";
import { isPromise } from "../../Entity";
import { navigateToRoute } from "../Router/MicroMRouterState";
import { MenuItem } from "./MenuItem";

export interface MenuContentProps {
    client: MicroMClient,
    menuId: string,
    setContent: Dispatch<SetStateAction<ReactNode>>,
    setOpened: Dispatch<SetStateAction<boolean>>,
    isLoggedIn: boolean | undefined,
    setIsLoggedIn: Dispatch<SetStateAction<boolean | undefined>>,
    loggedInInfo?: Partial<MicroMClientClaimTypes>,
    theme?: MantineTheme
}

export interface UseMenuContentProps extends MenuContentProps {
    menuContent: (props: MenuContentProps) => MenuItem[],
    enableMenuSecurity?: boolean,
    defaultLoadingComponent?: ReactNode,
    searchMenuItemsWithContentOnly?: boolean,

}

export const UseMenuContentDefaultProps: Partial<UseMenuContentProps> = {
    enableMenuSecurity: true,
    defaultLoadingComponent: <Skeleton />,
    searchMenuItemsWithContentOnly: true
}

export interface MenuItemActionProps {
    item: MenuItem,
    setContent: Dispatch<SetStateAction<ReactNode>>,
    setOpened: Dispatch<SetStateAction<boolean>>,
    clearContent: boolean,
    defaultLoadingComponent?: ReactNode,
    onActiveChange: React.Dispatch<SetStateAction<string>>,
    onSubitemActiveChange: React.Dispatch<SetStateAction<string>>,
    autoHideNavBarOnClick?: boolean
}

const triggerItemAction = async (props: MenuItemActionProps) => {
    const { item, setOpened, onActiveChange, onSubitemActiveChange, autoHideNavBarOnClick } = props;
    if (!item.noActive) {
        // MMC: TODO, change the activeIDState to a single state
        onActiveChange(item.ID);
        onSubitemActiveChange(item.ID);
    }
    else {
        onActiveChange('');
        onSubitemActiveChange('');
    }

    if (item.onClick) {
        item.onClick();
    }

    if ((item.content || item.subitems) && item.menuPath) {
        navigateToRoute(item.menuPath);
    }

    if (autoHideNavBarOnClick && !item.subitems) setOpened(false);
}

const populateMenuPaths = (
    items: MenuItem[],
    parentPath: string = '',
    parentDescription: string = ''
) => {
    items.forEach(item => {
        const currentPath = `${parentPath}/${item.ID}`;
        item.menuPath = currentPath;

        const currentDescription = parentDescription
            ? `${parentDescription} / ${item.label}`
            : item.label;

        item.menuPathDescription = currentDescription;

        if (item.subitems && item.subitems.length > 0) {
            populateMenuPaths(item.subitems, currentPath, currentDescription);
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

const filterEnabledItems = (
    items: MenuItem[],
    menuId: string,
    enabled: Set<string>
): MenuItem[] => {
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
        })
};

const CreateSpotlightActions = (items: MenuItem[], baseActionProps: Omit<MenuItemActionProps, 'item' | 'autoHideNavBarOnClick'>, searchMenuItemsWithContentOnly?: boolean, actions: SpotlightAction[] = []): SpotlightAction[] => {
    for (const item of items) {
        if (item.section === 'items') {
            const group = item.menuPathDescription
                ?.split("/")
                .map((part) => part.trim())
                .filter(Boolean)[0];

            if (!searchMenuItemsWithContentOnly || (searchMenuItemsWithContentOnly && (item.content || item.onClick))) {
                actions.push({
                    id: item.ID,
                    title: item.label,
                    description: `${item.menuPathDescription}${item.description ? ` - ${item.description}` : ""}`,
                    group: group,
                    icon: item.icon,
                    onTrigger: () => triggerItemAction({ ...baseActionProps, item }),
                });
            }
            if (item.subitems) {
                CreateSpotlightActions(item.subitems, baseActionProps, searchMenuItemsWithContentOnly, actions);
            }
        }
    }
    return actions;
}

export function useMenuContent(props: UseMenuContentProps) {
    const {
        client, setContent, isLoggedIn, setIsLoggedIn, menuContent, defaultLoadingComponent, setOpened, loggedInInfo, menuId,
        enableMenuSecurity, searchMenuItemsWithContentOnly
    } = useComponentDefaultProps('useMenuContent', UseMenuContentDefaultProps, props);

    const activeIDState = useState<string>('');
    const [, setActiveID] = activeIDState;
    const subitemActiveIDState = useState<string>('');
    const [, setSubitemActiveID] = subitemActiveIDState;
    const [items, setItems] = useState<MenuItem[]>([]);
    const [actions, setActions] = useState<SpotlightAction[]>([]);
    const [menuDictionary, setMenuDictionary] = useState<Record<string, MenuItem>>({});


    const internal_items = useMemo(
        () => {
            const items = menuContent({ client, setContent, setOpened, isLoggedIn, setIsLoggedIn, loggedInInfo, menuId });
            populateMenuPaths(items);
            return items;
        },
        [menuContent, client, setContent, setOpened, isLoggedIn, setIsLoggedIn, loggedInInfo, menuId]
    );

    // MMC: returns an array of mantine SpotLightAction from items
    const internal_actions = useMemo(() => {
        const baseActionProps = { setContent, setOpened, clearContent: true, defaultLoadingComponent, onActiveChange: setActiveID, onSubitemActiveChange: setSubitemActiveID };

        const actionList = CreateSpotlightActions(internal_items, baseActionProps, searchMenuItemsWithContentOnly);

        return actionList;
    }, [setContent, setOpened, defaultLoadingComponent, setActiveID, setSubitemActiveID, internal_items, searchMenuItemsWithContentOnly]);

    useEffect(() => {
        const get = async () => {
            if (isLoggedIn) {
                if (enableMenuSecurity) {
                    const enabled = await client.getMenus();
                    const enabled_items = filterEnabledItems(internal_items, menuId, enabled);

                    const enabled_actions = internal_actions.filter(item => enabled.has(`${menuId}_${item.id}`));

                    const dictionary = createMenuDictionary(enabled_items);
                    setMenuDictionary(dictionary);

                    setItems(enabled_items);
                    setActions(enabled_actions);
                }
                else {
                    const dictionary = createMenuDictionary(internal_items);
                    setMenuDictionary(dictionary);

                    setItems(internal_items);
                    setActions(internal_actions);
                }
            }
            else {
                setItems([]);
                setActions([]);
                setMenuDictionary({});
            }
        }

        get();

    }, [client, isLoggedIn, menuId, enableMenuSecurity]);

    const result = useMemo(() => ({
        activeIDState,
        subitemActiveIDState,
        items,
        actions,
        menuPathsDictionary: menuDictionary
    }), [activeIDState, subitemActiveIDState, items, actions, menuDictionary]);

    return result;

}

export type UseMenuContentReturnType = ReturnType<typeof useMenuContent>;