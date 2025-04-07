import { useCallback } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { FormMode, MicroMModalSize, openEntityForm, useModal } from "../Core";

// MMC: TODO parcel BUG, if imported in the same line when using the library it breaks
// import { ImportEntityData, ImportEntityDataFormPropsImportEntityDataFormProps } from "../../DataDictionary/ImportEntityData";
import { ImportEntityData } from "../../DataDictionary/ImportEntityData/ImportEntityData";
import { ImportEntityDataFormProps } from "../../DataDictionary/ImportEntityData/ImportEntityDataForm";
import { OperationStatus } from "../../client/OperationStatus";
import { DBStatusResult } from "../../client/client.types";

export interface UseImportDataProps {
    initialFormMode: FormMode,
    title?: string,
    element?: HTMLElement,
    getDataOnInit?: boolean
    modalFormSize?: MicroMModalSize,
    handleModalSaved: (newStatus: OperationStatus<DBStatusResult | null>) => Promise<void>,
    handleModalCancel: () => Promise<void>,
    handleModalClosed?: () => void,
}


export function useImportDataForm({
    initialFormMode, title, element, getDataOnInit, modalFormSize,
    handleModalCancel, handleModalClosed, handleModalSaved
}: UseImportDataProps) {
    const modals = useModal();


    const openImportDataForm = useCallback(async (importEntity: Entity<EntityDefinition>) => {
        if (!importEntity) return;

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
                importEntity
            }
        });
    }, [element, getDataOnInit, handleModalCancel, handleModalClosed, handleModalSaved, initialFormMode, modalFormSize, modals, title]);

    return {
        openImportDataForm
    };
}

