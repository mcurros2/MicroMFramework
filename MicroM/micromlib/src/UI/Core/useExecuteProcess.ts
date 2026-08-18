import { useCallback, useEffect, useRef, useState } from "react";
import { DBStatus, DBStatusResult, OperationStatus, toDBStatusMicroMError, toMicroMError, ValuesObject } from "../../client";
import { Entity, EntityDefinition, EntityProc } from "../../Entity";
import { areExecuteProcRequestsEqual, copyRequestValues, ExecuteInFlight, ExecuteProcRequest } from "./executeRequest";

interface DBStatusError {
    Errors: DBStatus[];
}

function isDBStatusError(error: unknown): error is DBStatusError {
    return typeof error === 'object' &&
        error !== null &&
        'Errors' in error &&
        Array.isArray(error.Errors);
}

export function useExecuteProcess(entity: Entity<EntityDefinition>, proc: EntityProc) {
    const initialStatus: OperationStatus<DBStatusResult> = { loading: false, operationType: 'proc' };
    const [status, setStatus] = useState<OperationStatus<DBStatusResult>>(initialStatus);

    const statusRef = useRef(status);
    const mounted = useRef(false);
    const inFlight = useRef<ExecuteInFlight<ExecuteProcRequest>>();
    const lastSuccessfulRequest = useRef<ExecuteProcRequest>();

    const updateStatus = useCallback((newStatus: OperationStatus<DBStatusResult>) => {
        statusRef.current = newStatus;
        if (mounted.current) setStatus(newStatus);
    }, []);

    useEffect(() => {
        mounted.current = true;

        return () => {
            mounted.current = false;

            const current = inFlight.current;
            if (current) {
                inFlight.current = undefined;
                current.controller.abort("Component unmounted");
            }
        };
    }, []);

    const execute = useCallback(async (values?: ValuesObject, force_refresh = false) => {
        const request: ExecuteProcRequest = {
            entity,
            proc,
            values: copyRequestValues(values)
        };

        const current = inFlight.current;

        if (current) {
            console.warn("useExecuteProcess ignored execute(): a request is already in progress. Call abort() before executing again.");
            return statusRef.current;
        }

        if (!force_refresh && areExecuteProcRequestsEqual(lastSuccessfulRequest.current, request)) {
            return;
        }

        const controller = new AbortController();
        const token = Symbol("useExecuteProcess request");

        inFlight.current = { ...request, controller, token };
        updateStatus({ loading: true, operationType: 'proc' });

        try {
            const data = await entity.API.executeProcess(proc, values, controller.signal);

            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            lastSuccessfulRequest.current = request;
            const newStatus: OperationStatus<DBStatusResult> = { data, operationType: 'proc' };
            updateStatus(newStatus);

            return newStatus;
        }
        catch (e: unknown) {
            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            const newStatus: OperationStatus<DBStatusResult> = {
                error: isDBStatusError(e) ? toDBStatusMicroMError(e.Errors, 'add') : toMicroMError(e),
                operationType: 'proc'
            };
            updateStatus(newStatus);

            return newStatus;
        }
        finally {
            if (inFlight.current?.token === token) {
                inFlight.current = undefined;
            }
        }
    }, [entity, proc, updateStatus]);

    const abort = useCallback(() => {
        const current = inFlight.current;
        if (!current) return;

        inFlight.current = undefined;
        current.controller.abort();
        updateStatus({ loading: false, operationType: 'proc' });
    }, [updateStatus]);

    return {
        execute,
        status,
        abort
    }
}
