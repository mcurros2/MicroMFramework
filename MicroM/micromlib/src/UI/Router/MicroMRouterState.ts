export interface NavigationState {
    navigated: boolean;
    route: string;
}

export interface MicroMRouterState {
    path: string;
    navigate: (newPath: string) => void;
    navigationState: NavigationState;
}