import { useCallback, useEffect, useState } from 'react';
import { MicroMRouterState, NavigationState } from './MicroMRouterState';

// MMC: normalize the path to always start with '/#/', the recieved path can start with '/#/', with '/' or without any of them
const normalizePath = (path: string) => {
    if (path.startsWith('#/')) {
        return path.slice(1);
    }
    return path;
}

export function useNavigation(): MicroMRouterState {
    // Adjust initial path setup to check for '/#/' prefix
    const initialPath = window.location.hash.startsWith('#/') ? window.location.hash.slice(1) : '/';
    const [path, setPath] = useState(initialPath);

    // Initialize navigated to false to indicate initial load
    const [navigationState, setNavigationState] = useState<NavigationState>({ navigated: false, route: path });

    // Unified navigation handling
    const handleNavigation = useCallback((newPath: string) => {
        const formattedPath = normalizePath(newPath);
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

    const navigate = useCallback((newPath: string) => {
        // Values set up here will include the #. If newpath has no / it will be added (.hash works like that)
        window.location.hash = normalizePath(newPath);
    }, []);

    return { path, navigate, navigationState };
};
