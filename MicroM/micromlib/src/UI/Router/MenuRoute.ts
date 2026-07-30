import { decodeBase64Url, encodeBase64Url, ValuesObject } from "../../client";
import { isValuesObject } from "../../Entity/ValuesObjectFunctions";

export const MENU_ROUTE_CONTEXT_VERSION = 1 as const;

export interface MenuRouteContext {
    version: typeof MENU_ROUTE_CONTEXT_VERSION;
    parentKeys: ValuesObject;
    contextLabel: string;
    originPath: string;
}

export interface MenuRoute {
    menuId: string;
    itemPath: string;
    context?: MenuRouteContext;
}

export interface CreateMenuRouteProps {
    menuId: string;
    itemPath?: string;
    context?: Omit<MenuRouteContext, "version">;
}

export function getMenuBasePath(menuId: string) {
    return `/${encodeURIComponent(menuId)}`;
}

export function createMenuRoute({ menuId, itemPath, context }: CreateMenuRouteProps) {
    const pathSegments = itemPath?.split("/").filter(Boolean) ?? [];
    if (pathSegments[0] && decodeURIComponent(pathSegments[0]) === menuId) pathSegments.shift();
    const relativeItemPath = pathSegments.join("/");
    const path = `${getMenuBasePath(menuId)}${relativeItemPath ? `/${relativeItemPath}` : ""}`;

    if (!context) return path;

    const payload: MenuRouteContext = {
        version: MENU_ROUTE_CONTEXT_VERSION,
        ...context
    };
    return `${path}?${new URLSearchParams({
        context: encodeBase64Url(JSON.stringify(payload))
    }).toString()}`;
}

export function parseMenuRoute(route: string): MenuRoute | null {
    try {
        const [path, search = ""] = route.replace(/^#/, "").split("?", 2);
        const segments = path.split("/").filter(Boolean);
        if (!segments[0]) return null;

        const encodedContext = new URLSearchParams(search).get("context");
        let context: MenuRouteContext | undefined;

        if (encodedContext !== null) {
            const payload = JSON.parse(decodeBase64Url(encodedContext)) as Partial<MenuRouteContext>;
            if (
                payload.version !== MENU_ROUTE_CONTEXT_VERSION ||
                !isValuesObject(payload.parentKeys) ||
                typeof payload.contextLabel !== "string" ||
                typeof payload.originPath !== "string" ||
                !payload.originPath.startsWith("/")
            ) {
                return null;
            }
            context = payload as MenuRouteContext;
        }

        return {
            menuId: decodeURIComponent(segments[0]),
            itemPath: segments.length > 1 ? `/${segments.slice(1).join("/")}` : "/",
            context
        };
    } catch {
        return null;
    }
}
