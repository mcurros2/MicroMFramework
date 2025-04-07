import { useEffect, useRef, useState } from "react";
import { EntityColumnFlags, areValuesObjectsEqual } from "../../Entity";
import * as cf from "../../Entity/ColumnsFunctions";
import { OperationStatus, ValuesObject, toMicroMError } from "../../client";
import { UseLookupEntityOptions, useLookupEntity } from "../Lookup";

export function useExecuteLookup({ entity, lookupDefName, parentKeys }: UseLookupEntityOptions) {
    const { lookupEntity } = useLookupEntity({ entity, lookupDefName, parentKeys });

    const [status, setStatus] = useState<OperationStatus<string>>({ loading: true, operationType: 'lookup' });
    const cancellation = useRef<AbortController>(new AbortController());
    const done = useRef<boolean>(false);
    const prevValues = useRef<ValuesObject | undefined>();

    useEffect(() => {
        return () => {
            if (!done.current) {
                console.log("useExecuteLookup aborted on unmount");
                cancellation.current.abort("Component unmounted");
            }
        };
    }, []);

    const execute = async () => {
        if (!entity || !lookupEntity) return;

        // Abort the previous request before starting a new one
        cancellation.current.abort("ExecuteLookup, aborting previous request.");
        cancellation.current = new AbortController();
        done.current = false;

        // Set parentKeys
        cf.setValues(lookupEntity.def.columns, parentKeys, null, true);

        const current_values = cf.getValuesObject(lookupEntity.def.columns, { flags: EntityColumnFlags.pk | EntityColumnFlags.fk, ignoreDefaults: false });

        if (
            !areValuesObjectsEqual(current_values, prevValues.current)
        ) {

            try {
                if (entity && lookupEntity) {
                    setStatus({ loading: true, operationType: 'lookup' });

                    const data = await lookupEntity.API.lookupData(cancellation.current.signal);
                    done.current = true;
                    const status_result: OperationStatus<string> = { data: data, operationType: 'lookup' };
                    setStatus(status_result);
                    return status_result;
                }

                // Update previous values
                prevValues.current = cf.getValuesObject(lookupEntity.def.columns, { flags: EntityColumnFlags.pk | EntityColumnFlags.fk, ignoreDefaults: false });

            }

            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            catch (e: any) {
                if (e.name !== 'AbortError' && e !== "ExecuteLookup, aborting previous request.") {
                    const error_result: OperationStatus<string> = { error: toMicroMError(e), operationType: 'lookup' };
                    setStatus(error_result);
                    return error_result;
                }
            }

        }
    }

    return {
        execute: execute,
        status: status
    }

}
