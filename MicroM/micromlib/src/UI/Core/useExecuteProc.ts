import { useCallback, useEffect, useRef, useState } from "react";
import { DataResult, OperationStatus, toMicroMError, ValuesObject } from "../../client";
import { Entity, EntityDefinition, EntityProc } from "../../Entity";
import { areExecuteProcRequestsEqual, copyRequestValues, ExecuteInFlight, ExecuteProcRequest } from "./executeRequest";

export function useExecuteProc(entity: Entity<EntityDefinition>, proc: EntityProc) {
    const initialStatus: OperationStatus<DataResult[]> = { loading: false, operationType: 'proc' };
    const [status, setStatus] = useState<OperationStatus<DataResult[]>>(initialStatus);
    const statusRef = useRef(status);
    const mounted = useRef(false);
    const inFlight = useRef<ExecuteInFlight<ExecuteProcRequest>>();
    const lastSuccessfulRequest = useRef<ExecuteProcRequest>();

    const updateStatus = useCallback((newStatus: OperationStatus<DataResult[]>) => {
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
            console.warn("useExecuteProc ignored execute(): a request is already in progress. Call abort() before executing again.");
            return statusRef.current;
        }

        if (!force_refresh && areExecuteProcRequestsEqual(lastSuccessfulRequest.current, request)) {
            return;
        }

        const controller = new AbortController();
        const token = Symbol("useExecuteProc request");

        inFlight.current = { ...request, controller, token };
        updateStatus({ loading: true, operationType: 'proc' });

        try {
            const data = await entity.API.executeProc(proc, values, controller.signal);

            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            lastSuccessfulRequest.current = request;
            const newStatus: OperationStatus<DataResult[]> = { data, operationType: 'proc' };
            updateStatus(newStatus);

            return newStatus;
        }
        catch (e: unknown) {
            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            const newStatus: OperationStatus<DataResult[]> = { error: toMicroMError(e), operationType: 'proc' };
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
