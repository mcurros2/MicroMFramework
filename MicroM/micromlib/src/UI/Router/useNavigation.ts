import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { MicroMRouterState, navigateToRoute, NavigationState, normalizeRoutePath, splitRoute } from './MicroMRouterState';
import { canNavigateLocally } from './NavigationGuards';

export function useNavigation(): MicroMRouterState {
    // Adjust initial path setup to check for '/#/' prefix
    const initialRoute = window.location.hash.startsWith('#/') ? window.location.hash.slice(1) : '/';
    const [route, setRoute] = useState(initialRoute);
    const { path, searchParams } = useMemo(() => splitRoute(route), [route]);

    // Initialize navigated to false to indicate initial load
    const [navigationState, setNavigationState] = useState<NavigationState>({ navigated: false, route: path });
    const navigationPendingRef = useRef(false);

    // Unified navigation handling
    const handleNavigation = useCallback((newPath: string) => {
        const formattedPath = normalizeRoutePath(newPath);

        if (formattedPath !== route) { // Check to prevent unnecessary state updates
            setRoute(formattedPath);
            setNavigationState({ navigated: true, route: formattedPath });
        }

    }, [route]);

    useEffect(() => {
        let active = true;

        // Update path and navigationState based on direct hash changes
        const restoreCurrentRoute = () => {
            const currentUrl = `${window.location.pathname}${window.location.search}#${route}`;
            window.history.replaceState(window.history.state, '', currentUrl);
        };

        const handleHashChange = async () => {
            const newPath = normalizeRoutePath(window.location.hash.slice(1));
            if (newPath === route) return;

            if (navigationPendingRef.current) {
                restoreCurrentRoute();
                return;
            }

            navigationPendingRef.current = true;
            try {
                const canNavigate = await canNavigateLocally({ currentRoute: route, nextRoute: newPath });
                if (!active) return;
                if (canNavigate) handleNavigation(newPath);
                else restoreCurrentRoute();
            }
            finally {
                navigationPendingRef.current = false;
            }
        };

        window.addEventListener('hashchange', handleHashChange);
        return () => {
            active = false;
            window.removeEventListener('hashchange', handleHashChange);
        };
    }, [handleNavigation, route]);

    return { path, route, searchParams, navigate: navigateToRoute, navigationState };
};
