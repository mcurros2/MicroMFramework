import { useCallback, useEffect, useRef, useState } from "react";
import { DBStatus, DBStatusResult, OperationStatus, toDBStatusMicroMError, toMicroMError, ValuesObject } from "../../client";
import { areValuesObjectsEqual, Entity, EntityDefinition, EntityProc } from "../../Entity";

export function useExecuteProcess(entity: Entity<EntityDefinition>, proc: EntityProc) {
    const [status, setStatus] = useState<OperationStatus<DBStatusResult>>({ loading: false, operationType: 'proc' });
    const cancellation = useRef<AbortController>(new AbortController());
    const done = useRef<boolean>(false);
    const prevValues = useRef<ValuesObject | undefined>();

    useEffect(() => {
        return () => {
            if (!done.current) {
                console.log("useExecuteProcess aborted on unmount");
                cancellation.current.abort("Component unmounted");
            }
        };
    }, [entity, proc]);

    const execute = useCallback(async (values?: ValuesObject) => {
        if (status.loading) {
            return status;
        }

        if (!areValuesObjectsEqual(values, prevValues.current)) {
            // Abort the previous request before starting a new one
            cancellation.current.abort("ExecuteProcess, aborting previous request.");
            cancellation.current = new AbortController();
            done.current = false;

            try {
                if (entity && proc) {
                    setStatus({ loading: true, operationType: 'proc' });

                    const data = await entity.API.executeProcess(proc, values, cancellation.current.signal);
                    done.current = true;

                    const new_status: OperationStatus<DBStatusResult> = { data: data, operationType: 'proc' };
                    setStatus(new_status);

                    // Update previous values
                    prevValues.current = values;

                    return new_status;
                }
            }
            catch (e: any) {
                if (e.name !== 'AbortError' && e !== "ExecuteProcess, aborting previous request.") {
                    const new_status: OperationStatus<DBStatusResult> = {
                        error: e.Errors ? toDBStatusMicroMError(e.Errors as DBStatus[], 'add') : toMicroMError(e),
                        operationType: 'proc'
                    };
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
