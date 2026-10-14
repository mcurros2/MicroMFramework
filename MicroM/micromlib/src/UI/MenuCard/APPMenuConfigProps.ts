import type { ComponentType, ReactNode } from "react";
import { MicroMClient } from "../../client/MicromClient";
import { MenuConfigItem, MenuContentResult } from "../Menu/useMenuContent";

export interface MenuHostProps {
    client: MicroMClient,
    mainMenuId: string,
    menuId: string,
    menu: MenuContentResult,
    menus: Record<string, MenuContentResult>,
    emptyNodeMessage?: ReactNode,
    defaultLoadingComponent?: ReactNode,
    renderBreadcrumbs?: boolean,
}

export interface APPMenuConfigProps extends MenuConfigItem {
    hostPanel?: ComponentType<MenuHostProps>;
}
