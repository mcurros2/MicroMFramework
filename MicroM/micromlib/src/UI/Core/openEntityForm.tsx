import { Text } from "@mantine/core";
import { Entity, EntityDefinition } from "../../Entity";
import { DBStatusResult, OperationStatus } from "../../client";
import { MicroMModalSize, ModalContextType } from "./ModalsManager";
import { createEntityForm } from "./createEntityForm";
import { FormOptions } from "./types";

export interface openEntityFormProps<T extends FormOptions<Entity<EntityDefinition>>> {
    modals: ModalContextType,
    title?: string,
    element?: HTMLElement,
    handleModalSaved: (newStatus: OperationStatus<DBStatusResult | null>) => Promise<void>,
    handleModalCancel: () => Promise<void>,
    handleModalClosed?: () => void,
    modalFormSize?: MicroMModalSize,
    withModalFullscreenButton?: boolean,
    formProps: T
}

export async function openEntityForm<T extends FormOptions<Entity<EntityDefinition>>>({
    modals, title, element, handleModalCancel, handleModalSaved, modalFormSize, formProps,
    handleModalClosed, withModalFullscreenButton
}: openEntityFormProps<T>) {

    withModalFullscreenButton = withModalFullscreenButton === undefined ? true : withModalFullscreenButton;
    const showOK = formProps.initialFormMode !== 'view';
    const onSaved = formProps.initialFormMode !== 'view' ? (new_status: OperationStatus<DBStatusResult | null>) => handleModalSaved(new_status) : () => Promise.resolve();
    const onCancel = () => handleModalCancel();
    const entity_form = await createEntityForm<T>({
        showOK,
        onSaved,
        onCancel,
        ...formProps
    });

    await modals.open({
        modalProps: {
            title: <Text fw="700">{title} {formProps.entity.Title}</Text>,
            size: modalFormSize,
            withFullscreenButton: withModalFullscreenButton,
            closeOnClickOutside: false,
            closeOnEscape: true
        },
        onClosed: handleModalClosed,
        focusOnClosed: element,
        content: entity_form
    });

}
