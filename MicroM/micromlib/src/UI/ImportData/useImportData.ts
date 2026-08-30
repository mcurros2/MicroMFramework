import { useCallback, useEffect, useRef, useState } from "react";
import { DBStatus, FileImportMapping, OperationStatus, toDBStatusMicroMError, toMicroMError } from "../../client";
import { ImpDataResult } from "../../client/ImpDataResult";
import { Entity, EntityDefinition } from "../../Entity";

interface ImportInFlight {
    controller: AbortController,
    token: symbol,
}

export function useImportData(importEntity?: Entity<EntityDefinition>) {
    const initialStatus: OperationStatus<ImpDataResult> = { loading: false, operationType: 'import' };
    const [importStatus, setImportStatus] = useState<OperationStatus<ImpDataResult>>(initialStatus);

    const statusRef = useRef(importStatus);
    const mounted = useRef(false);
    const inFlight = useRef<ImportInFlight>();
    const cancellation = useRef<AbortController>(new AbortController());

    const updateStatus = useCallback((newStatus: OperationStatus<ImpDataResult>) => {
        statusRef.current = newStatus;
        if (mounted.current) setImportStatus(newStatus);
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

    const execute = useCallback(async (
        fileprocess_id: string,
        importProcedureName?: string,
        fileImportMapping?: FileImportMapping,
        initialRow?: number
    ) => {
        if (!importEntity || !fileprocess_id) return;

        const current = inFlight.current;
        if (current) {
            console.warn("useImportData ignored execute(): an import is already in progress. Call cancel() before executing again.");
            return statusRef.current;
        }

        const controller = new AbortController();
        const token = Symbol("useImportData request");
        cancellation.current = controller;
        inFlight.current = { controller, token };
        updateStatus({ loading: true, operationType: 'import' });

        try {
            const data = await importEntity.API.importData(
                controller.signal,
                importProcedureName ?? null,
                importEntity.parentKeys,
                fileprocess_id,
                fileImportMapping,
                initialRow
            );

            if (controller.signal.aborted || inFlight.current?.token !== token) return;

            const newStatus: OperationStatus<ImpDataResult> = { data, operationType: 'import' };
            updateStatus(newStatus);
            return newStatus;
        }
        catch (error: unknown) {
            if (controller.signal.aborted || inFlight.current?.token !== token) return;

            const importError = error as { Errors?: DBStatus[] };
            const newStatus: OperationStatus<ImpDataResult> = {
                error: importError.Errors
                    ? toDBStatusMicroMError(importError.Errors, 'add')
                    : toMicroMError(error),
                operationType: 'import'
            };
            updateStatus(newStatus);
            return newStatus;
        }
        finally {
            if (inFlight.current?.token === token) {
                inFlight.current = undefined;
            }
        }
    }, [importEntity, updateStatus]);

    const cancel = useCallback(() => {
        const current = inFlight.current;
        if (!current) return;

        inFlight.current = undefined;
        current.controller.abort();
        updateStatus({ loading: false, operationType: 'import' });
    }, [updateStatus]);

    return {
        execute,
        importStatus,
        get cancellation() {
            return cancellation.current;
        },
        cancel
    };
}
