import { useCallback, useEffect, useState } from 'react';
import { MicroMRouterState, navigateToRoute, NavigationState, normalizeRoutePath } from './MicroMRouterState';

export function useNavigation(): MicroMRouterState {
    // Adjust initial path setup to check for '/#/' prefix
    const initialPath = window.location.hash.startsWith('#/') ? window.location.hash.slice(1) : '/';
    const [path, setPath] = useState(initialPath);

    // Initialize navigated to false to indicate initial load
    const [navigationState, setNavigationState] = useState<NavigationState>({ navigated: false, route: path });

    // Unified navigation handling
    const handleNavigation = useCallback((newPath: string) => {
        const formattedPath = normalizeRoutePath(newPath);
        if (formattedPath !== path) { // Check to prevent unnecessary state updates
            setPath(formattedPath);
            setNavigationState({ navigated: true, route: formattedPath });
        }
    }, [path]);

    useEffect(() => {
        // Update path and navigationState based on direct hash changes
        const handleHashChange = () => {
            const newPath = window.location.hash.slice(1);
            handleNavigation(newPath);
        };

        window.addEventListener('hashchange', handleHashChange);
        return () => window.removeEventListener('hashchange', handleHashChange);
    }, [handleNavigation]);

    return { path, navigate: navigateToRoute, navigationState };
};
