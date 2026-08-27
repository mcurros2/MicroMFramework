import { useCallback } from "react";
import { DBStatusResult } from "../../client/client.types";
import { OperationStatus } from "../../client/OperationStatus";
// MMC: TODO parcel BUG, if imported in the same line when using the library it breaks
// import { ImportEntityData, ImportEntityDataFormPropsImportEntityDataFormProps } from "../../DataDictionary/ImportEntityData";
import { ImportEntityData } from "../../DataDictionary/ImportEntityData/ImportEntityData";
import { ImportEntityDataFormProps } from "../../DataDictionary/ImportEntityData/ImportEntityDataForm";
import { Entity, EntityDefinition } from "../../Entity";
import { FormMode, MicroMModalSize, openEntityForm, useModal } from "../Core";

export interface UseImportDataProps {
    initialFormMode: FormMode,
    title?: string,
    element?: HTMLElement,
    getDataOnInit?: boolean
    modalFormSize?: MicroMModalSize,
    handleModalSaved: (newStatus: OperationStatus<DBStatusResult | null>) => Promise<void>,
    handleModalCancel: () => Promise<void>,
    handleModalClosed?: () => void,
    handleImportSuccess?: () => Promise<void>,
}


export function useImportDataForm({
    initialFormMode, title, element, getDataOnInit, modalFormSize,
    handleModalCancel, handleModalClosed, handleModalSaved, handleImportSuccess
}: UseImportDataProps) {
    const modals = useModal();


    const openImportDataForm = useCallback(async (importEntity: Entity<EntityDefinition>, entityProcName?: string, excludedImportDestinations?: string[]) => {
        if (!importEntity) return;

        if (entityProcName && !importEntity.def.procs[entityProcName]) {
            console.warn(`Import data: procedure '${entityProcName}' was not found in entity '${importEntity.name}'.`);
            return;
        }

        const importData = new ImportEntityData(importEntity.API.client);

        await openEntityForm<ImportEntityDataFormProps>({
            modals,
            title,
            element,
            handleModalCancel,
            handleModalSaved,
            handleModalClosed,
            modalFormSize,
            formProps: {
                entity: importData,
                initialFormMode,
                getDataOnInit,
                importEntity,
                entityProcName,
                excludedImportDestinations,
                onImportSuccess: handleImportSuccess
            }
        });
    }, [element, getDataOnInit, handleImportSuccess, handleModalCancel, handleModalClosed, handleModalSaved, initialFormMode, modalFormSize, modals, title]);

    return {
        openImportDataForm
    };
}

