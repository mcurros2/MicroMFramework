import { useMicroMRouter } from "./useMicroMRouter";

export interface RouteProps {
    path: string;
    children: React.ReactNode;
}

export function Route (props: RouteProps) {
    const { path: currentPath } = useMicroMRouter();
    const { path, children } = props;

    return currentPath === path ? <>{children}</> : null;
};
