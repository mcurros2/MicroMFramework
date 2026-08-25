import { useCallback, useEffect, useRef } from "react";
import { createMenuRoute, parseMenuRoute } from "../../Router/MenuRoute";
import { navigateToRoute } from "../../Router/MicroMRouterState";
import { useMicroMRouter } from "../../Router/useMicroMRouter";

export type MenuModalCloseBehavior = {
    route?: string;
    onClosed?: () => void;
    lastLevel?: boolean;
};

export function useMenuModalCloseNavigator(closeBehavior?: MenuModalCloseBehavior): () => void {
    const { route } = useMicroMRouter();
    const routeRef = useRef(route);
    routeRef.current = route;

    const mountedRef = useRef(true);
    useEffect(() => () => { mountedRef.current = false; }, []);

    return useCallback(() => {
        closeBehavior?.onClosed?.();

        if (!mountedRef.current) return;

        if (closeBehavior?.route) {
            navigateToRoute(closeBehavior.route);
            return;
        }

        if (closeBehavior?.lastLevel ?? true) {
            const parsed = parseMenuRoute(routeRef.current);
            if (!parsed) return;

            const segments = parsed.itemPath.split("/").filter(Boolean);
            segments.pop();
            const parentItemPath = segments.length ? `/${segments.join("/")}` : "/";

            navigateToRoute(createMenuRoute({ menuId: parsed.menuId, itemPath: parentItemPath, context: parsed.context }));
        }
    }, [closeBehavior]);
}
