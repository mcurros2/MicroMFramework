import { useEffect, useRef, useState } from "react";
import { Entity, EntityDefinition, areArraysContentsEqual, areValuesObjectsEqual } from "../../Entity";
import { DataResult, OperationStatus, ValuesObject, toMicroMError } from "../../client";

export function useExecuteView(
    entity?: Entity<EntityDefinition>, values?: ValuesObject, viewName?: string, search?: string[] | undefined, limit?: string | null, refresh?: boolean, filters?: ValuesObject
) {
    const [status, setStatus] = useState<OperationStatus<DataResult[]>>({ loading: true, operationType: 'view' });
    const cancellation = useRef<AbortController>(new AbortController());
    const done = useRef<boolean>();
    const prevValues = useRef<ValuesObject | undefined>(values);
    const prevSearch = useRef<string[] | undefined>(search);
    const prevLimit = useRef<string | null | undefined>(limit);
    const prevRefresh = useRef<boolean | undefined>(undefined);

    useEffect(() => {
        return () => {
            if (done.current === false) {
                console.log("useExecuteView aborted");
                cancellation.current?.abort("ExecuteView Effect cleanup");
            }
        };
    }, []);

    useEffect(() => {
        if (!entity || !viewName) return;

        const mergedValues = { ...values, ...filters ?? {} };

        // Check if values, search, limit or refresh have changed from their previous values
        if (
            !areValuesObjectsEqual(mergedValues, prevValues.current) ||
            !areArraysContentsEqual(search, prevSearch.current) ||
            limit !== prevLimit.current ||
            refresh !== prevRefresh.current
        ) {

            cancellation.current.abort("ExecuteView, aborting previous request.");
            cancellation.current = new AbortController();
            done.current = false;

            async function getData() {
                setStatus({ loading: true, operationType: 'view' });
                try {
                    if (entity && viewName) {
                        const data = await entity.API.executeView(entity.def.views[viewName], mergedValues, limit, search, cancellation.current.signal);
                        done.current = true;

                        setStatus({ data: data, operationType: 'view' });
                    }

                }

                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                catch (e: any) {
                    if (e.name !== 'AbortError' && e !== "ExecuteView, aborting previous request.") {
                        setStatus({ error: toMicroMError(e), operationType: 'view' });
                    }
                }
            }

            getData();
        }

        // Update previous values
        prevValues.current = mergedValues;
        prevSearch.current = search;
        prevLimit.current = limit;
        prevRefresh.current = refresh;

    }, [entity, filters, limit, refresh, search, values, viewName]);

    return status;
}
