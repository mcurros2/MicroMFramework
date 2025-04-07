import { useCallback, useEffect, useRef, useState } from "react";
import { Entity, EntityDefinition, areValuesObjectsEqual } from "../../Entity";
import { OperationStatus, ValuesObject, toMicroMError } from "../../client";

export type useExecuteActionReturnType<TReturn extends EntityDefinition | ValuesObject> = {
    status: OperationStatus<TReturn>
    execute: (values?: ValuesObject) => Promise<OperationStatus<TReturn> | undefined>
}

export function useExecuteServerAction<T extends EntityDefinition, TReturn extends EntityDefinition | ValuesObject>(
    entity: Entity<T>,
    actionName: string
): useExecuteActionReturnType<TReturn> {
    const [status, setStatus] = useState<OperationStatus<TReturn>>({ loading: true });
    const cancellation = useRef<AbortController>(new AbortController());
    const done = useRef<boolean>(false);
    const prevValues = useRef<ValuesObject | undefined>();

    useEffect(() => {
        return () => {
            if (!done.current) {
                console.log("useExecuteServerAction aborted on unmount");
                cancellation.current.abort("Component unmounted");
            }
        };
    }, []);

    const execute = useCallback(async (values?: ValuesObject) => {
        if (status.loading) {
            return status;
        }

        if (!areValuesObjectsEqual(values, prevValues.current)) {
            // Abort the previous request before starting a new one
            cancellation.current.abort("ExecuteServerAction, aborting previous request.");
            cancellation.current = new AbortController();
            done.current = false;

            try {
                const action = entity.def.serverActions[actionName];
                if (!action || !action.valuesMapper) {
                    throw new Error('Action or valuesMapper missing.');
                }

                setStatus({ loading: true, operationType: 'action' });

                const data = await entity.API.executeServerAction<ValuesObject>(action, values, cancellation.current.signal);
                done.current = true;

                const new_values = action.valuesMapper(data);
                setStatus({ data: new_values as TReturn, operationType: 'action' });

                // Update previous values
                prevValues.current = values;
            }
            catch (e: any) {
                if (e.name !== 'AbortError' && e !== "ExecuteServerAction, aborting previous request.") {
                    const errorResult: OperationStatus<TReturn> = {
                        error: toMicroMError(e),
                        operationType: 'action'
                    };
                    setStatus(errorResult);
                }
            }
        }
    }, [entity, actionName, status]);

    return { status, execute };
}
