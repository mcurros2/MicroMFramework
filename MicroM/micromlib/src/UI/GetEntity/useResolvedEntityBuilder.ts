import { useEffect, useRef, useState } from "react";
import { MicroMClient } from "../../client/MicromClient";
import { ValuesObject } from "../../client/client.types";
import { EntitySource } from "./GetEntity";

export interface UseResolvedEntityBuilderResult<TResult> {
    result: TResult | null;
    ready: boolean;
}

export function useResolvedEntityBuilder<TResult>(client: MicroMClient, parentKeys: ValuesObject | undefined, source: EntitySource<TResult>): UseResolvedEntityBuilderResult<TResult> {
    const { entityConstructor, entityLoader } = source;

    const resultRef = useRef<TResult | null>(null);

    const [ready, setReady] = useState(false);

    useEffect(() => {
        let mounted = true;

        if (resultRef.current) {
            setReady(true);
            return;
        }

        if (entityConstructor) {
            resultRef.current = entityConstructor(client, parentKeys);
            setReady(true);
            return;
        }

        if (entityLoader) {
            entityLoader(client, parentKeys).then(result => {
                if (!mounted) return;

                resultRef.current = result;
                setReady(true);
            });
        }

        return () => {
            mounted = false;
        };
        // The source functions are intentionally excluded from the dependencies: call sites
        // recreate them on every render and the resolved result is cached in resultRef.
    }, [client, parentKeys]);

    return { result: ready ? resultRef.current : null, ready };
}
