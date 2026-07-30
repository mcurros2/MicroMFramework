import { ReactElement, ReactNode } from "react";
import { ValuesObject } from "../../client";

export interface MenuItemContext {
    menuId: string,
    parentKeys: ValuesObject
}

export type MenuItemContent =
    | ReactNode
    | Promise<ReactNode>
    | ((context: MenuItemContext) => ReactNode | Promise<ReactNode>);

export interface MenuItem {
    ID: string,
    link?: string,
    label: string,
    labelComponent?: ReactNode,
    icon?: ReactNode,
    description?: string,
    notifications?: number,
    subitems?: MenuItem[],
    rightSection?: ReactElement,
    noActive?: boolean,
    loadingComponent?: ReactNode,
    content?: MenuItemContent,
    onClick?: (context?: MenuItemContext) => void | Promise<void>,
    section: 'header' | 'items' | 'footer',
    canShowAsShortcut?: boolean,
    menuPath?: string,
    menuPathDescription?: string,
    parentKeys?: ValuesObject,
    menuId?: string
}

export function getMenuItemContext(item: MenuItem, context?: MenuItemContext): MenuItemContext {
    return {
        menuId: context?.menuId ?? item.menuId ?? '',
        parentKeys: {
            ...(item.parentKeys ?? {}),
            ...(context?.parentKeys ?? {})
        }
    };
}

export function resolveMenuItemContent(item: MenuItem, context?: MenuItemContext): ReactNode | Promise<ReactNode> | undefined {
    return typeof item.content === 'function'
        ? item.content(getMenuItemContext(item, context))
        : item.content;
}
