import { Text } from "@mantine/core";
import { useCallback } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { DBStatusResult, OperationStatus } from "../../client";
import { MicroMModalSize, useModal } from "./ModalsManager";
import { createEntityForm } from "./createEntityForm";
import { FormMode } from "./types";

export interface OpenFormProps {
    entity: Entity<EntityDefinition>,
    initialFormMode: FormMode,
    title?: string,
    element?: HTMLElement,
    getDataOnInit?: boolean,
    onModalSaved?: (new_status: OperationStatus<DBStatusResult | null>) => void,
    onModalCancelled?: () => void,
    onModalClosed?: () => void,
    modalFormSize?: MicroMModalSize,
    OKText?: string,
    CancelText?: string,
    showCancel?: boolean,
    otherFormProps?: any,
    dontAddEntityTitle?: boolean,
    withFullscreenButton?: boolean,
    closeOnEscape?: boolean,
    closeOnClickOutside?: boolean,
}

export function useOpenForm() {

    const modals = useModal();

    const openForm = useCallback(async (props: OpenFormProps) => {
        const {
            entity, initialFormMode, getDataOnInit, modalFormSize, title, element, onModalSaved, onModalCancelled, OKText, CancelText
            , showCancel, onModalClosed, otherFormProps, dontAddEntityTitle, withFullscreenButton, closeOnEscape, closeOnClickOutside
        } = props;

        const showOK = initialFormMode !== 'view';

        const onSaved = initialFormMode !== 'view' ? async (new_status: OperationStatus<DBStatusResult | null>) => {
            await modals.close();
            if (onModalSaved) await onModalSaved(new_status);
        } : () => Promise.resolve();

        const onCancel = async () => {
            await modals.close();
            if (onModalCancelled) await onModalCancelled();
        };

        const onClosed = async () => {
            if (onModalClosed) await onModalClosed();
        };

        const entity_form = createEntityForm({ entity, initialFormMode, getDataOnInit, showOK, onSaved, onCancel, OKText, CancelText, showCancel, ...otherFormProps });

        await modals.open({
            modalProps: {
                title: <Text fw="700">{title && title}{!dontAddEntityTitle && ` ${entity.Title}`}</Text>,
                size: modalFormSize,
                withFullscreenButton: withFullscreenButton,
                closeOnEscape: closeOnEscape,
                closeOnClickOutside: closeOnClickOutside
            },
            focusOnClosed: element,
            content: entity_form,
            onClosed: onClosed
        });

    }, [modals]);

    return openForm;
}