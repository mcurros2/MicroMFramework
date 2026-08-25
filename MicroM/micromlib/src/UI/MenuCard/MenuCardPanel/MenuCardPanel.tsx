import { useComponentDefaultProps } from "@mantine/core";
import { ReactNode, useMemo } from "react";
import { MenuItem, MenuItemContext } from "../../Menu/MenuItem";
import { MenuContentResult } from "../../Menu/useMenuContent";
import { createMenuRoute, parseMenuRoute } from "../../Router/MenuRoute";
import { useMicroMRouter } from "../../Router/useMicroMRouter";
import { getBreadcrumbsForPath, MenuCardBreadcrumbsProps } from "../MenuCardBreadcrumbs/MenuCardBreadcrumbProps";
import { MenuCardContentPanel } from "../MenuCardContentPanel/MenuCardContentPanel";
import { MenuCardRoot } from "../MenuCardRoot/MenuCardRoot";

export interface MenuCardPanelProps {
    menuId: string;
    menu: MenuContentResult;
    breadcrumbsPrefix?: MenuCardBreadcrumbsProps["items"];
    renderBreadcrumbs?: boolean;
    emptyNodeMessage?: ReactNode;
    defaultLoadingComponent?: ReactNode;
}

export const MenuCardPanelDefaultProps: Partial<MenuCardPanelProps> = {
    breadcrumbsPrefix: [],
    renderBreadcrumbs: true,
};

export function MenuCardPanel(props: MenuCardPanelProps) {
    const { menuId, menu, breadcrumbsPrefix, renderBreadcrumbs, emptyNodeMessage, defaultLoadingComponent } =
        useComponentDefaultProps('MenuCardPanel', MenuCardPanelDefaultProps, props);
    const { route } = useMicroMRouter();
    const menuRoute = useMemo(() => parseMenuRoute(route), [route]);

    const itemContext = useMemo(
        () => menuRoute?.context
            ? ({ menuId, parentKeys: menuRoute.context.parentKeys } satisfies MenuItemContext)
            : undefined,
        [menuId, menuRoute?.context]
    );

    if (!menuRoute || menuRoute.menuId !== menuId) {
        return <>{emptyNodeMessage}</>;
    }

    const menuPath = menuRoute.itemPath === "/"
        ? `/${menuId}`
        : `/${menuId}${menuRoute.itemPath}`;

    let selectedItem: MenuItem[] | null = null;
    if (menuRoute.itemPath === "/" && menu.items.length > 0) {
        selectedItem = menu.items;
    } else {
        const foundItem = menu.menuPathsDictionary[menuPath];
        selectedItem = foundItem ? [foundItem] : null;
    }

    if (!selectedItem) {
        return <>{emptyNodeMessage}</>;
    }

    const routeBuilder = (itemPath: string) => createMenuRoute({
        menuId,
        itemPath,
        context: menuRoute.context
    });

    const breadcrumbs = [
        ...breadcrumbsPrefix!,
        ...getBreadcrumbsForPath(menuPath, menu.menuPathsDictionary, routeBuilder)
    ];

    if (selectedItem.length > 1 || selectedItem[0].subitems) {
        return (
            <MenuCardRoot
                key={`rt${route}`}
                items={selectedItem}
                rootID={menuRoute.itemPath === "/" ? menuPath : selectedItem[0].ID}
                breadcrumbs={breadcrumbs}
                renderBreadcrumbs={renderBreadcrumbs}
                routeBuilder={routeBuilder}
            />
        );
    }

    if (selectedItem[0].content) {
        return (
            <MenuCardContentPanel
                key={`cn${route}`}
                item={selectedItem[0]}
                itemContext={itemContext}
                breadcrumbs={breadcrumbs}
                renderBreadcrumbs={renderBreadcrumbs}
                emptyNodeMessage={emptyNodeMessage}
                defaultLoadingComponent={defaultLoadingComponent}
            />
        );
    }

    return <>{emptyNodeMessage}</>;
}
