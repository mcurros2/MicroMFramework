import { ReactNode } from "react";
import { MicroMRouterContext } from "./useMicroMRouter";
import { useNavigation } from "./useNavigation";

export function MicroMRouter(props: { children: ReactNode }) {
    const router = useNavigation();
    const { children } = props;

    return <MicroMRouterContext.Provider value={router}>{children}</MicroMRouterContext.Provider>;
};