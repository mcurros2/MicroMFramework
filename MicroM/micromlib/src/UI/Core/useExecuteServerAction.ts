import { useCallback, useEffect, useRef, useState } from "react";
import { OperationStatus, toMicroMError, ValuesObject } from "../../client";
import { areValuesObjectsEqual, Entity, EntityDefinition } from "../../Entity";
import { copyRequestValues, ExecuteInFlight, ExecuteRequest } from "./executeRequest";

export type useExecuteServerActionReturnType<TReturn extends ValuesObject> = {
    status: OperationStatus<TReturn>
    execute: (values?: ValuesObject) => Promise<OperationStatus<TReturn> | undefined>
    abort: () => void
}

interface ExecuteServerActionRequest<T extends EntityDefinition> extends ExecuteRequest {
    entity: Entity<T>;
    actionName: string;
}

function areRequestsEqual<T extends EntityDefinition>(
    a: ExecuteServerActionRequest<T> | undefined,
    b: ExecuteServerActionRequest<T>
): boolean {
    return a?.entity === b.entity &&
        a.actionName === b.actionName &&
        areValuesObjectsEqual(a.values, b.values);
}

export function useExecuteServerAction<T extends EntityDefinition, TReturn extends ValuesObject>(
    entity: Entity<T>, actionName: string, doNotExecuteIfEntityValuesUnchanged?: boolean
): useExecuteServerActionReturnType<TReturn> {
    const [status, setStatus] = useState<OperationStatus<TReturn>>({ loading: false });
    const statusRef = useRef(status);
    const mounted = useRef(false);
    const inFlight = useRef<ExecuteInFlight<ExecuteServerActionRequest<T>>>();
    const lastSuccessfulRequest = useRef<ExecuteServerActionRequest<T>>();

    const updateStatus = useCallback((newStatus: OperationStatus<TReturn>) => {
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
        const current = inFlight.current;
        if (current) {
            console.warn("useExecuteServerAction ignored execute(): a request is already in progress. Call abort() before executing again.");
            return statusRef.current;
        }

        const request: ExecuteServerActionRequest<T> = {
            entity,
            actionName,
            values: copyRequestValues(values)
        };

        if (doNotExecuteIfEntityValuesUnchanged && areRequestsEqual(lastSuccessfulRequest.current, request)) {
            return;
        }

        const controller = new AbortController();
        const token = Symbol("useExecuteServerAction request");
        inFlight.current = { ...request, controller, token };

        try {
            const action = entity.def.serverActions[actionName];
            if (!action) {
                throw new Error('Action or valuesMapper missing.');
            }

            updateStatus({ loading: true, operationType: 'action' });

            const data = await entity.API.executeServerAction<TReturn>(action, values, controller.signal);

            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            lastSuccessfulRequest.current = request;
            updateStatus({ data, operationType: 'action' });
        }
        catch (e: unknown) {
            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            const errorResult: OperationStatus<TReturn> = {
                error: toMicroMError(e),
                operationType: 'action'
            };
            updateStatus(errorResult);
        }
        finally {
            if (inFlight.current?.token === token) {
                inFlight.current = undefined;
            }
        }
    }, [actionName, doNotExecuteIfEntityValuesUnchanged, entity, updateStatus]);

    const abort = useCallback(() => {
        const current = inFlight.current;
        if (!current) return;

        inFlight.current = undefined;
        current.controller.abort();
        updateStatus({ loading: false });
    }, [updateStatus]);

    return { status, execute, abort };
}
