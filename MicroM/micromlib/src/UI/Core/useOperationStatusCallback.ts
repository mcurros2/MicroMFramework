import { useCallback, useState } from "react";
import { DBStatus, DataOperationType, OperationStatus, isDBStatusResult, toDBStatusMicroMError, toMicroMError } from "../../client";
import { FormMode } from "./types";

export interface UseOperationStatusCallback<T> {
    callback: (...args: any[]) => Promise<T>,
    operation: DataOperationType,
    deps: React.DependencyList
}

export type UseOperationStatusCallbackReturnType<T> = {
    operationCallback: (...args: any[]) => Promise<OperationStatus<T>>,
    status: OperationStatus<T>
}

export function useOperationStatusCallback<T>(props: UseOperationStatusCallback<T>): UseOperationStatusCallbackReturnType<T> {
    const { callback, deps, operation } = props;
    const [status, setStatus] = useState<OperationStatus<T>>({});

    const result = useCallback(async (...args: any[]) => {
        setStatus({ loading: true, operationType: operation });
        try {

            const data = await callback(...args);
            const new_status: OperationStatus<T> = { loading: false, data: data, operationType: operation };
            if (isDBStatusResult(data) && data.Failed) {
                new_status.error = toDBStatusMicroMError(data.Results as DBStatus[]);
            }
            setStatus(new_status);
            return new_status;
        }
        catch (e: any) {
            if (e.name !== 'AbortError') {
                const new_status: OperationStatus<T> = { error: e.Errors ? toDBStatusMicroMError(e.Errors as DBStatus[], operation as FormMode) :  toMicroMError(e), operationType: operation };
                setStatus(new_status);
                return new_status;
            }
            else {
                return { loading: false, operationType: operation }
            }
        }
    }, [callback, operation, ...deps]);

    return { operationCallback: result, status: status };
}