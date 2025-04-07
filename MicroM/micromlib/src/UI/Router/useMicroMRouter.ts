import { createContext, useContext } from "react";
import { MicroMRouterState } from "./MicroMRouterState";

export const MicroMRouterContext = createContext<MicroMRouterState | undefined>(undefined);

export const useMicroMRouter = () => {
    const context = useContext(MicroMRouterContext);
    if (context === undefined) {
        throw new Error('useMicroMRouter must be used within a RouterProvider');
    }
    return context;
};
