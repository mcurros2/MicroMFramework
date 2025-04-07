import { useCallback, useEffect, useRef, useState } from "react";
import { Entity, EntityDefinition, EntityProc, areValuesObjectsEqual } from "../../Entity";
import { DataResult, OperationStatus, ValuesObject, toMicroMError } from "../../client";

export function useExecuteProc(entity: Entity<EntityDefinition>, proc: EntityProc) {
    const [status, setStatus] = useState<OperationStatus<DataResult[]>>({ loading: false, operationType: 'proc' });
    const cancellation = useRef<AbortController>(new AbortController());
    const done = useRef<boolean>(false);
    const prevValues = useRef<ValuesObject | undefined>();

    useEffect(() => {
        return () => {
            if (!done.current) {
                console.log("useExecuteProc aborted on unmount");
                cancellation.current.abort("Component unmounted");
            }
        };
    }, []);

    const execute = useCallback(async (values?: ValuesObject) => {
        if (status.loading) return status;

        if (!areValuesObjectsEqual(values, prevValues.current)) {
            // Abort the previous request before starting a new one
            cancellation.current.abort("ExecuteProc, aborting previous request.");
            cancellation.current = new AbortController();
            done.current = false;

            try {
                // Update previous values
                prevValues.current = values;

                if (entity && proc) {
                    setStatus({ loading: true, operationType: 'proc' });

                    const data = await entity.API.executeProc(proc, values, cancellation.current.signal);
                    done.current = true;

                    const new_status: OperationStatus<DataResult[]> = { data: data, operationType: 'proc' };
                    setStatus(new_status);
                    return new_status;
                }

            }
            catch (e: any) {
                if (e.name !== 'AbortError' && e !== "ExecuteProc, aborting previous request.") {
                    const new_status: OperationStatus<DataResult[]> = { error: toMicroMError(e), operationType: 'proc' };
                    setStatus(new_status);
                    return new_status;
                }
            }
        }
    }, [entity, proc, status]);

    return {
        execute: execute,
        status: status
    }
}
