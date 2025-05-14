import { useComponentDefaultProps } from "@mantine/core";
import { useEffect, useRef } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { DBStatusResult, MicroMClient, OperationStatus } from "../../client";
import { FormMode, MicroMModalSize, useOpenForm } from "../Core";


export interface EntityFormModalProps {
    client: MicroMClient,
    entityConstructor: (client: MicroMClient) => Entity<EntityDefinition>,
    initialFormMode?: FormMode,
    getDataOnInit?: boolean,
    openState: boolean,
    setOpenState: (open: boolean) => void,
    onModalClosed?: () => void,
    onModalSaved?: (status: OperationStatus<DBStatusResult | null>) => Promise<void>,
    modalFormSize?: MicroMModalSize,
    withFullscreenButton?: boolean,
    closeOnEscape?: boolean,
    closeOnClickOutside?: boolean,
}

export const EntityFormModalDefaultProps: Partial<EntityFormModalProps> = {
    initialFormMode: 'add',
    modalFormSize: 'xl',
    withFullscreenButton: true,
    closeOnEscape: true,
    closeOnClickOutside: false,
}

export function EntityFormModal(props: EntityFormModalProps) {
    const {
        client, entityConstructor, openState, setOpenState, initialFormMode, getDataOnInit,
        onModalClosed, onModalSaved, modalFormSize, withFullscreenButton, closeOnClickOutside, closeOnEscape
    } = useComponentDefaultProps('EntityFormModal', EntityFormModalDefaultProps, props);

    const openForm = useOpenForm();

    const hasOpened = useRef(false);

    useEffect(() => {
        const open = async () => {
            const entity = entityConstructor(client);
            await openForm({
                modalFormSize,
                entity: entity,
                initialFormMode: initialFormMode!,
                getDataOnInit: getDataOnInit,
                withFullscreenButton,
                closeOnClickOutside,
                closeOnEscape,
                onModalClosed: () => {
                    setOpenState(false);
                    hasOpened.current = false;
                    if(onModalClosed) onModalClosed();
                },
                onModalSaved: async (status: OperationStatus<DBStatusResult | null>) => {
                    if (onModalSaved) await onModalSaved(status);
                }
            });
        }

        if (openState && !hasOpened.current) {
            hasOpened.current = true;
            open();
        }

    }, [client, entityConstructor, getDataOnInit, initialFormMode, onModalClosed, onModalSaved, openForm, openState, setOpenState, modalFormSize, withFullscreenButton, closeOnEscape, closeOnClickOutside]);

    return null;
}