import { MantineTheme, Skeleton, useComponentDefaultProps } from "@mantine/core";
import { SpotlightAction } from "@mantine/spotlight";
import { Dispatch, ReactNode, SetStateAction, useEffect, useMemo, useRef, useState } from "react";
import { MicroMClient, MicroMClientClaimTypes } from "../../client";
import { navigateToRoute } from "../Router/MicroMRouterState";
import { getMenuItemContext, MenuItem } from "./MenuItem";

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
    searchMenuItemsWithContentOnly?: boolean
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
        await item.onClick(getMenuItemContext(item));
    }

    if ((item.content || item.subitems) && item.menuPath) {
        navigateToRoute(item.menuPath);
    }

    if (autoHideNavBarOnClick && !item.subitems) setOpened(false);
}

const populateMenuPaths = (items: MenuItem[], parentPath: string = '', parentDescription: string = '', menuId: string = '') => {
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
        client, setContent, isLoggedIn, setIsLoggedIn, menuContent, setOpened, loggedInInfo, menuId,
        enableMenuSecurity, searchMenuItemsWithContentOnly
    } = useComponentDefaultProps('useMenuContent', UseMenuContentDefaultProps, props);

    const activeIDState = useState<string>('');
    const [, setActiveID] = activeIDState;
    const subitemActiveIDState = useState<string>('');
    const [, setSubitemActiveID] = subitemActiveIDState;
    const [items, setItems] = useState<MenuItem[]>([]);
    const [actions, setActions] = useState<SpotlightAction[]>([]);
    const [menuDictionary, setMenuDictionary] = useState<Record<string, MenuItem>>({});
    const [isLoading, setIsLoading] = useState(true);


    const internal_items = useMemo(
        () => {
            const items = menuContent({ client, setContent, setOpened, isLoggedIn, setIsLoggedIn, loggedInInfo, menuId });
            populateMenuPaths(items, `/${menuId}`, '', menuId);
            return items;
        },
        [menuContent, client, setContent, setOpened, isLoggedIn, setIsLoggedIn, loggedInInfo, menuId]
    );

    // MMC: returns an array of mantine SpotLightAction from items
    const internal_actions = useMemo(() => {
        const baseActionProps = { setContent, setOpened, clearContent: true, onActiveChange: setActiveID, onSubitemActiveChange: setSubitemActiveID };

        const actionList = CreateSpotlightActions(internal_items, baseActionProps, searchMenuItemsWithContentOnly);

        return actionList;
    }, [setContent, setOpened, setActiveID, setSubitemActiveID, internal_items, searchMenuItemsWithContentOnly]);

    // Menus are materialized when the login/security lifecycle changes. Keep the
    // latest static definitions available without making rendering inputs effect triggers.
    const internalItemsRef = useRef(internal_items);
    const internalActionsRef = useRef(internal_actions);
    internalItemsRef.current = internal_items;
    internalActionsRef.current = internal_actions;

    useEffect(() => {
        let mounted = true;

        const get = async () => {
            setIsLoading(true);
            try {
                const menuItems = internalItemsRef.current;
                const menuActions = internalActionsRef.current;

                if (isLoggedIn) {
                    if (enableMenuSecurity) {
                        const enabled = await client.getMenus();
                        const enabled_items = filterEnabledItems(menuItems, menuId, enabled);
                        const enabled_actions = menuActions.filter(item => enabled.has(`${menuId}_${item.id}`));

                        if (!mounted) return;
                        setMenuDictionary(createMenuDictionary(enabled_items));
                        setItems(enabled_items);
                        setActions(enabled_actions);
                    }
                    else {
                        if (!mounted) return;
                        setMenuDictionary(createMenuDictionary(menuItems));
                        setItems(menuItems);
                        setActions(menuActions);
                    }
                }
                else {
                    if (!mounted) return;
                    setItems([]);
                    setActions([]);
                    setMenuDictionary({});
                }
            }
            finally {
                if (mounted) setIsLoading(false);
            }
        }

        get();

        return () => {
            mounted = false;
        };
    }, [client, isLoggedIn, menuId, enableMenuSecurity]);

    const result = useMemo(() => ({
        activeIDState,
        subitemActiveIDState,
        items,
        actions,
        menuPathsDictionary: menuDictionary,
        isLoading
    }), [activeIDState, subitemActiveIDState, items, actions, menuDictionary, isLoading]);

    return result;

}

export type UseMenuContentReturnType = ReturnType<typeof useMenuContent>;
