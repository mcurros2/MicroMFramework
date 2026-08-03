import { useCallback, useEffect, useRef, useState } from "react";
import { OperationStatus, toMicroMError, ValuesObject } from "../../client";
import { Entity, EntityDefinition, EntityProc } from "../../Entity";
import { areExecuteProcRequestsEqual, copyRequestValues, ExecuteInFlight, ExecuteProcRequest } from "./executeRequest";

export function useExecuteExportProc(entity: Entity<EntityDefinition>, proc: EntityProc) {
    const initialStatus: OperationStatus<Blob> = { loading: false, operationType: 'export' };
    const [status, setStatus] = useState<OperationStatus<Blob>>(initialStatus);
    const statusRef = useRef(status);
    const mounted = useRef(false);
    const inFlight = useRef<ExecuteInFlight<ExecuteProcRequest>>();
    const lastSuccessfulRequest = useRef<ExecuteProcRequest>();

    const updateStatus = useCallback((newStatus: OperationStatus<Blob>) => {
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

    const execute = useCallback(async (values?: ValuesObject) => {
        const request: ExecuteProcRequest = {
            entity,
            proc,
            values: copyRequestValues(values)
        };

        const current = inFlight.current;

        if (current && areExecuteProcRequestsEqual(current, request)) {
            return statusRef.current;
        }

        if (!current && areExecuteProcRequestsEqual(lastSuccessfulRequest.current, request)) {
            return;
        }

        if (current) {
            inFlight.current = undefined;
            current.controller.abort("ExecuteExportProc, aborting previous request.");
        }

        const controller = new AbortController();
        const token = Symbol("useExecuteExportProc request");

        inFlight.current = { ...request, controller, token };
        updateStatus({ loading: true, operationType: 'export' });

        try {
            const data = await entity.API.exportProc(proc, values, controller.signal);

            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            lastSuccessfulRequest.current = request;
            const newStatus: OperationStatus<Blob> = { data, operationType: 'export' };
            updateStatus(newStatus);

            return newStatus;
        }
        catch (e: unknown) {
            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            const newStatus: OperationStatus<Blob> = { error: toMicroMError(e), operationType: 'export' };
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
        updateStatus({ loading: false, operationType: 'export' });
    }, [updateStatus]);

    return {
        execute,
        status,
        abort
    }
}
