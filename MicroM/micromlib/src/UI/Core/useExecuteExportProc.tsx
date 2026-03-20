import { useCallback, useEffect, useRef, useState } from "react";
import { OperationStatus, toMicroMError, ValuesObject } from "../../client";
import { areValuesObjectsEqual, Entity, EntityDefinition, EntityProc } from "../../Entity";

export function useExecuteExportProc(entity: Entity<EntityDefinition>, proc: EntityProc) {
    const [status, setStatus] = useState<OperationStatus<Blob>>({ loading: false, operationType: 'export' });
    const cancellation = useRef<AbortController>(new AbortController());
    const done = useRef<boolean>(false);
    const prevValues = useRef<ValuesObject | undefined>();

    useEffect(() => {
        return () => {
            if (!done.current) {
                console.log("useExecuteExportProc aborted on unmount");
                cancellation.current.abort("Component unmounted");
            }
        };
    }, []);

    const execute = useCallback(async (values?: ValuesObject) => {
        if (status.loading) return status;

        if (!areValuesObjectsEqual(values, prevValues.current)) {
            // Abort the previous request before starting a new one
            cancellation.current.abort("ExecuteExportProc, aborting previous request.");
            cancellation.current = new AbortController();
            done.current = false;

            try {
                // Update previous values
                prevValues.current = values;

                if (entity && proc) {
                    setStatus({ loading: true, operationType: 'export' });

                    const data = await entity.API.exportProc(proc, values, cancellation.current.signal);
                    done.current = true;

                    const new_status: OperationStatus<Blob> = { data: data, operationType: 'export' };
                    setStatus(new_status);
                    return new_status;
                }

            }
            catch (e: any) {
                if (e.name !== 'AbortError' && e !== "ExecuteExportProc, aborting previous request.") {
                    const new_status: OperationStatus<Blob> = { error: toMicroMError(e), operationType: 'export' };
                    setStatus(new_status);
                    return new_status;
                }
            }
        }
    }, [entity, proc, status]);

    const abort = useCallback(() => { cancellation.current.abort() }, []);

    return {
        execute: execute,
        status: status,
        abort: abort
    }
}
