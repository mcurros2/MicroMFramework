import { useCallback, useEffect, useRef, useState } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { DBStatus, OperationStatus, toDBStatusMicroMError, toMicroMError } from "../../client";
import { ImpDataResult } from "../../client/ImpDataResult";


export function useImportData(importEntity?: Entity<EntityDefinition>) {
    const cancellation = useRef<AbortController>(new AbortController());
    const done = useRef<boolean>(false);

    const [importStatus, setImportStatus] = useState<OperationStatus<ImpDataResult>>({ loading: false, operationType: 'import' });


    useEffect(() => {
        cancellation.current = new AbortController();
        done.current = false;
        return () => {
            if (!done.current) {
                cancellation.current?.abort("ImportData Effect cleanup");
            }
        };
    }, [importEntity]);


    const execute = useCallback(async (fileprocess_id: string) => {
        if (!importEntity || !fileprocess_id) return;

        try {
            setImportStatus({ loading: true });

            done.current = false;
            const result = await importEntity.API.importData(cancellation.current.signal, null, importEntity.parentKeys, fileprocess_id);
            done.current = true;

            const result_status: OperationStatus<ImpDataResult> = { data: result, operationType: 'import' };
            setImportStatus(result_status);

            return result_status;
        }
        catch (error: any) {
            if (error.name !== 'AbortError') {
                const new_status: OperationStatus<ImpDataResult> = { error: error.Errors ? toDBStatusMicroMError(error.Errors as DBStatus[], 'add') : toMicroMError(error), operationType: 'import' };
                setImportStatus(new_status);
                return new_status;
            }
            else {
                const new_status: OperationStatus<ImpDataResult> = { loading: false, operationType: 'import' };
                setImportStatus(new_status);
                return new_status;
            }
        }
    }, [importEntity]);

    return {
        execute,
        importStatus,
        cancellation: cancellation.current
    };
}

