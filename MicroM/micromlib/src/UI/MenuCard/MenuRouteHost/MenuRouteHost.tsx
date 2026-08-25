import { useComponentDefaultProps } from "@mantine/core";
import type { ReactNode } from "react";
import { useEffect, useMemo } from "react";
import { MicroMClient } from "../../../client/MicromClient";
import { getMainMenuId } from "../../Menu/getMainMenuId";
import { MenuContentResult } from "../../Menu/useMenuContent";
import { parseMenuRoute } from "../../Router/MenuRoute";
import { useMicroMRouter } from "../../Router/useMicroMRouter";
import type { APPMenuConfigProps } from "../APPMenuConfigProps";
import { MenuCardPanel } from "../MenuCardPanel/MenuCardPanel";

export interface MenuRouteHostProps {
    client: MicroMClient;
    menuConfig: Record<string, APPMenuConfigProps>;
    menus: Record<string, MenuContentResult>;
    emptyNodeMessage?: ReactNode;
    defaultLoadingComponent?: ReactNode;
}

export const MenuRouteHostDefaultProps: Partial<MenuRouteHostProps> = {};

export function MenuRouteHost(props: MenuRouteHostProps) {
    const { client, menuConfig, menus, emptyNodeMessage, defaultLoadingComponent } =
        useComponentDefaultProps('MenuRouteHost', MenuRouteHostDefaultProps, props);

    const { path, route, navigate } = useMicroMRouter();
    const menuRoute = useMemo(() => parseMenuRoute(route), [route]);

    const mainMenuId = useMemo(() => getMainMenuId(menuConfig), [menuConfig]);
    const isHomeAlias = path === "/";

    useEffect(() => {
        if (isHomeAlias) navigate(`/${mainMenuId}`);
    }, [isHomeAlias, navigate, mainMenuId]);

    if (isHomeAlias) return null;
    if (!menuRoute) return <>{emptyNodeMessage}</>;

    const menu = menus[menuRoute.menuId];
    const config = menuConfig[menuRoute.menuId];
    if (!menu || !config) return <>{emptyNodeMessage}</>;

    const hostProps = {
        client,
        mainMenuId,
        menuId: menuRoute.menuId,
        menu,
        menus,
        emptyNodeMessage,
        defaultLoadingComponent
    };

    if (config.hostPanel) {
        const HostPanel = config.hostPanel;
        return <HostPanel {...hostProps} />;
    }

    return (
        <MenuCardPanel
            menuId={hostProps.menuId}
            menu={hostProps.menu}
            emptyNodeMessage={hostProps.emptyNodeMessage}
            defaultLoadingComponent={hostProps.defaultLoadingComponent}
        />
    );
}
