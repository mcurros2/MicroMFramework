export interface NavigationState {
    navigated: boolean;
    route: string;
}

export interface MicroMRouterState {
    path: string;
    route: string;
    searchParams: URLSearchParams;
    navigate: (newPath: string) => void;
    navigationState: NavigationState;
}

// MMC: normalize the path to always start with '/#/', the recieved path can start with '/#/', with '/' or without any of them
export const normalizeRoutePath = (path: string) => {
    if (path.startsWith('#/')) {
        return decodeURIComponent(path.slice(1));
    }
    return decodeURIComponent(path);
}

export const navigateToRoute = (newPath: string) => {
    window.location.hash = normalizeRoutePath(newPath);
}

export const normalizeRouteURL = (path: string) => {
    if (!path.startsWith('/#')) {
        return `/#${path}`;
    }
    return path;
}

export function splitRoute(route: string) {
    const queryIndex = route.indexOf('?');
    const path = queryIndex >= 0 ? route.slice(0, queryIndex) : route;
    const search = queryIndex >= 0 ? route.slice(queryIndex + 1) : '';
    return { path, searchParams: new URLSearchParams(search) };
}

