import { useCallback, useEffect, useRef, useState } from "react";
import { OperationStatus, toMicroMError, ValuesObject } from "../../client";
import { areValuesObjectsEqual, copyValuesObject, Entity, EntityColumnFlags, EntityDefinition } from "../../Entity";
import * as cf from "../../Entity/ColumnsFunctions";
import { useLookupEntity, UseLookupEntityOptions } from "../Lookup";
import { ExecuteInFlight, ExecuteRequest } from "./executeRequest";

interface ExecuteLookupRequest extends ExecuteRequest {
    entity: Entity<EntityDefinition>;
    lookupEntity: Entity<EntityDefinition>;
    lookupProc?: string;
    values: ValuesObject;
}

function areRequestsEqual(a: ExecuteLookupRequest, b: ExecuteLookupRequest): boolean {
    return a.entity === b.entity &&
        a.lookupEntity === b.lookupEntity &&
        a.lookupProc === b.lookupProc &&
        areValuesObjectsEqual(a.values, b.values);
}

export function useExecuteLookup({ entity, lookupDefName, parentKeys }: UseLookupEntityOptions) {
    const { lookupEntity } = useLookupEntity({ entity, lookupDefName, parentKeys });

    const initialStatus: OperationStatus<string> = { loading: true, operationType: 'lookup' };
    const [status, setStatus] = useState<OperationStatus<string>>(initialStatus);
    const statusRef = useRef(status);
    const mounted = useRef(false);
    const inFlight = useRef<ExecuteInFlight<ExecuteLookupRequest>>();

    const lookupProc = entity.def.lookups[lookupDefName].proc;

    const updateStatus = useCallback((newStatus: OperationStatus<string>) => {
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

    const execute = useCallback(async () => {
        if (!entity || !lookupEntity) return;

        // Set parentKeys
        cf.setValues(lookupEntity.def.columns, parentKeys, null, true, true);

        const currentValues = cf.getValuesObject(
            lookupEntity.def.columns,
            { flags: EntityColumnFlags.pk | EntityColumnFlags.fk, ignoreDefaults: false }
        );

        const request: ExecuteLookupRequest = {
            entity,
            lookupEntity,
            lookupProc,
            values: copyValuesObject(currentValues)
        };

        const current = inFlight.current;

        if (current && areRequestsEqual(current, request)) {
            return statusRef.current;
        }

        if (current) {
            inFlight.current = undefined;
            current.controller.abort("ExecuteLookup, aborting previous request.");
        }

        const controller = new AbortController();
        const token = Symbol("useExecuteLookup request");
        inFlight.current = { ...request, controller, token };
        updateStatus({ loading: true, operationType: 'lookup' });

        try {
            const data = await lookupEntity.API.lookupData(controller.signal, null, lookupProc);

            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            const newStatus: OperationStatus<string> = { data, operationType: 'lookup' };
            updateStatus(newStatus);

            return newStatus;
        }
        catch (e: unknown) {
            if (controller.signal.aborted || inFlight.current?.token !== token) {
                return;
            }

            const newStatus: OperationStatus<string> = { error: toMicroMError(e), operationType: 'lookup' };
            updateStatus(newStatus);

            return newStatus;
        }
        finally {
            if (inFlight.current?.token === token) {
                inFlight.current = undefined;
            }
        }
    }, [entity, lookupEntity, lookupProc, parentKeys, updateStatus]);

    const abort = useCallback(() => {
        const current = inFlight.current;
        if (!current) return;

        inFlight.current = undefined;
        current.controller.abort();
        updateStatus({ loading: false, operationType: 'lookup' });
    }, [updateStatus]);

    return {
        execute,
        status,
        abort
    }

}
