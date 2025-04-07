import { useCallback, useLayoutEffect, useRef } from "react";

// MMC: this is specified in an RFC for react, to be in the next version
// this hook will fail if rendering, it should be used for events when callbacks can't provide a stable reference
export function useEvent<T extends (...args: any[]) => any>(callback: T): T {
    const callbackRef = useRef<T>(callback);

    useLayoutEffect(() => {
        callbackRef.current = callback;
    }, [callback]);

    return useCallback(
        (...args: Parameters<T>): ReturnType<T> => {
            return callbackRef.current(...args);
        },
        []
    ) as T;
}