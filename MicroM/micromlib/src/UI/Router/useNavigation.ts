import { useCallback, useEffect, useMemo, useState } from 'react';
import { MicroMRouterState, navigateToRoute, NavigationState, normalizeRoutePath, splitRoute } from './MicroMRouterState';

export function useNavigation(): MicroMRouterState {
    // Adjust initial path setup to check for '/#/' prefix
    const initialRoute = window.location.hash.startsWith('#/') ? window.location.hash.slice(1) : '/';
    const [route, setRoute] = useState(initialRoute);
    const { path, searchParams } = useMemo(() => splitRoute(route), [route]);

    // Initialize navigated to false to indicate initial load
    const [navigationState, setNavigationState] = useState<NavigationState>({ navigated: false, route: path });

    // Unified navigation handling
    const handleNavigation = useCallback((newPath: string) => {
        const formattedPath = normalizeRoutePath(newPath);
        if (formattedPath !== route) { // Check to prevent unnecessary state updates
            setRoute(formattedPath);
            setNavigationState({ navigated: true, route: formattedPath });
        }
    }, [route]);

    useEffect(() => {
        // Update path and navigationState based on direct hash changes
        const handleHashChange = () => {
            const newPath = window.location.hash.slice(1);
            handleNavigation(newPath);
        };

        window.addEventListener('hashchange', handleHashChange);
        return () => window.removeEventListener('hashchange', handleHashChange);
    }, [handleNavigation]);

    return { path, route, searchParams, navigate: navigateToRoute, navigationState };
};
