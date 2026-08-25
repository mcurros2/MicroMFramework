import { Stack, StackProps, useComponentDefaultProps } from "@mantine/core";
import type { ReactNode } from "react";
import { useMemo } from "react";
import { createMenuRoute, parseMenuRoute } from "../../Router/MenuRoute";
import { normalizeRouteURL } from "../../Router/MicroMRouterState";
import { useMicroMRouter } from "../../Router/useMicroMRouter";
import type { MenuHostProps } from "../APPMenuConfigProps";
import { getBreadcrumbsForPath, MenuCardBreadcrumbsProps } from "../MenuCardBreadcrumbs/MenuCardBreadcrumbProps";
import { MenuCardBreadcrumbs } from "../MenuCardBreadcrumbs/MenuCardBreadcrumbs";
import { MenuCardGrid } from "../MenuCardGrid/MenuCardGrid";
import { MenuCardPanel } from "../MenuCardPanel/MenuCardPanel";

export interface GestionarPanelProps extends MenuHostProps, Omit<StackProps, 'children'> {
    header?: ReactNode;
    homeLabel?: string;
}

export const GestionarPanelDefaultProps: Partial<GestionarPanelProps> = {
    homeLabel: 'Home',
    h: '100%',
};

export function ContextualMenuPanel(props: GestionarPanelProps) {
    const {
        header, mainMenuId, menuId, menu, menus, emptyNodeMessage, defaultLoadingComponent, homeLabel, ...others
    } = useComponentDefaultProps('GestionarPanel', GestionarPanelDefaultProps, props);

    const { route } = useMicroMRouter();
    const menuRoute = useMemo(() => parseMenuRoute(route), [route]);
    const context = menuRoute?.menuId === menuId ? menuRoute.context : undefined;

    if (!menuRoute || !context) {
        return <>{emptyNodeMessage}</>;
    }

    const originRoute = parseMenuRoute(context.originPath);
    const originMenu = originRoute ? menus[originRoute.menuId] : undefined;

    const menuPath = menuRoute.itemPath === "/"
        ? `/${menuId}`
        : `/${menuId}${menuRoute.itemPath}`;

    const routeBuilder = (itemPath: string) => createMenuRoute({
        menuId,
        itemPath,
        context
    });

    const breadcrumbs: MenuCardBreadcrumbsProps["items"] = [
        { title: homeLabel!, path: normalizeRouteURL(`/${mainMenuId}`) },
        ...(originRoute && originMenu
            ? getBreadcrumbsForPath(context.originPath, originMenu.menuPathsDictionary)
            : []),
        {
            title: context.contextLabel,
            path: normalizeRouteURL(createMenuRoute({ menuId, context }))
        },
        ...getBreadcrumbsForPath(menuPath, menu.menuPathsDictionary, routeBuilder)
    ];

    return (
        <Stack {...others}>
            <MenuCardBreadcrumbs items={breadcrumbs} menuID={menuId} />
            {header}
            {menuRoute.itemPath === "/"
                ? <MenuCardGrid items={menu.items} routeBuilder={routeBuilder} />
                : <MenuCardPanel
                    menuId={menuId}
                    menu={menu}
                    renderBreadcrumbs={false}
                    emptyNodeMessage={emptyNodeMessage}
                    defaultLoadingComponent={defaultLoadingComponent}
                />
            }
        </Stack>
    );
}
