import { BreadcrumbsProps, DefaultMantineColor, MenuProps, TitleOrder } from "@mantine/core";
import { ReactNode } from "react";
import { MenuItem } from "../../Menu/MenuItem";
import { normalizeRouteURL } from "../../Router/MicroMRouterState";

export interface MenuCardBreadcrumbsProps extends Omit<BreadcrumbsProps, 'children'> {
    items: { title: string, path: string }[],
    menuID: string,
    maxItems?: number,
    titleSize?: TitleOrder,
    textColor?: DefaultMantineColor | 'dimmed',
    /** Node shown in place of the collapsed items */
    collapsedIndicator?: ReactNode,
    /** Props for the dropdown menu that lists the collapsed items. Replaces the default object entirely when set per instance */
    collapsedMenuProps?: Partial<Omit<MenuProps, 'children'>>,
}

export function getBreadcrumbsForPath(
    path: string,
    menuPathsDictionary: Record<string, MenuItem>,
    routeBuilder: (path: string) => string = path => path
): MenuCardBreadcrumbsProps['items'] {
    const breadcrumbs: MenuCardBreadcrumbsProps['items'] = [];

    path.split('/').reduce((acc, segment) => {
        const currentPath = segment === '' ? acc : acc + '/' + segment;
        if (menuPathsDictionary[currentPath]) {
            breadcrumbs.push({ title: menuPathsDictionary[currentPath].label, path: normalizeRouteURL(routeBuilder(currentPath)) });
        }
        return currentPath;
    }, '');

    return breadcrumbs;
}