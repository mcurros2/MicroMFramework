import { MantineTheme, Skeleton, useComponentDefaultProps } from "@mantine/core";
import { SpotlightAction } from "@mantine/spotlight";
import { Dispatch, ReactNode, SetStateAction, useCallback, useEffect, useMemo, useState } from "react";
import { isPromise } from "../../Entity";
import { MicroMClient, MicroMClientClaimTypes } from "../../client";
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
    defaultLoadingComponent?: ReactNode
}

export const UseMenuContentDefaultProps: Partial<UseMenuContentProps> = {
    enableMenuSecurity: true,
    defaultLoadingComponent: <Skeleton />
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
    const { item, setContent, setOpened, clearContent, onActiveChange, onSubitemActiveChange, defaultLoadingComponent, autoHideNavBarOnClick } = props;
    if (!item.noActive) {
        // MMC: TODO, change the activeIDState to a single state
        onActiveChange(item.ID);
        onSubitemActiveChange(item.ID);
    }
    else {
        onActiveChange('');
        onSubitemActiveChange('');
    }

    if (item.content) {
        if (isPromise(item.content)) {
            setContent(item.loadingComponent ?? defaultLoadingComponent);
            const menuContent = await item.content;
            setContent(menuContent);
        }
        else {
            setContent(item.content);
        }
    }
    else {
        if (clearContent) setContent(<></>);
    }

    if (item.onClick) {
        item.onClick();
    }

    if (autoHideNavBarOnClick && !item.subitems) setOpened(false);

}

export function useMenuContent(props: UseMenuContentProps) {
    const {
        client, setContent, isLoggedIn, setIsLoggedIn, menuContent, defaultLoadingComponent, setOpened, loggedInInfo, menuId,
        enableMenuSecurity
    } = useComponentDefaultProps('useMenuContent', UseMenuContentDefaultProps, props);

    const activeIDState = useState<string>('');
    const [, setActiveID] = activeIDState;
    const subitemActiveIDState = useState<string>('');
    const [, setSubitemActiveID] = subitemActiveIDState;
    const [items, setItems] = useState<MenuItem[]>([]);
    const [actions, setActions] = useState<SpotlightAction[]>([]);

    const filterEnabledItems = useCallback((
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
            // MMC: leave the items without subitems as can be valid options
            //.filter(item => !(item.subitems && item.subitems.length === 0));
    }, []);

    const internal_items = useMemo(
        () => {
            return menuContent({ client, setContent, setOpened, isLoggedIn, setIsLoggedIn, loggedInInfo, menuId })
        },
        [menuContent, client, setContent, setOpened, isLoggedIn, setIsLoggedIn, loggedInInfo, menuId]
    );

    // MMC: defines a function called getSpotlightActions that returns an array of mantine SpotLightAction from items
    const internal_actions = useMemo(() => internal_items.flatMap((item) => {
        const baseActionProps = { setContent, setOpened, clearContent: true, defaultLoadingComponent, onActiveChange: setActiveID, onSubitemActiveChange: setSubitemActiveID };
        let actionList: SpotlightAction[] = [];

        if (item.section === 'items' && !item.subitems) {
            actionList.push({
                id: item.ID,
                target: `#${item.ID}`,
                title: item.label,
                description: item.description,
                icon: item.icon,
                onTrigger: () => triggerItemAction({ ...baseActionProps, item }),
            });
        }

        if (item.subitems) {
            actionList = actionList.concat(item.subitems.map(subitem => ({
                id: subitem.ID,
                target: `#${subitem.ID}`,
                title: subitem.label,
                description: subitem.description,
                icon: subitem.icon,
                onTrigger: () => triggerItemAction({ ...baseActionProps, item: subitem }),
            })));
        }

        return actionList;
    }), [internal_items, setContent, setOpened, defaultLoadingComponent, setActiveID, setSubitemActiveID]);


    useEffect(() => {
        const get = async () => {
            if (isLoggedIn) {
                if (enableMenuSecurity) {
                    const enabled = await client.getMenus();

                    const enabled_items = filterEnabledItems(internal_items, menuId, enabled);

                    const enabled_actions = internal_actions.filter(item => enabled.has(`${menuId}_${item.id}`));

                    setItems(enabled_items);
                    setActions(enabled_actions);
                }
                else {
                    setItems(internal_items);
                    setActions(internal_actions);
                 }
            }
            else {
                setItems([]);
                setActions([]);
            }
        }

        get();

    }, [client, isLoggedIn, menuId, enableMenuSecurity]);


    const result = useMemo(() => ({
        activeIDState,
        subitemActiveIDState,
        items,
        actions
    }), [activeIDState, subitemActiveIDState, items, actions]);

    return result;

}