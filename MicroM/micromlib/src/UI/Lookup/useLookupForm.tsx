﻿import { Text } from "@mantine/core";
import { ModalSettings } from "@mantine/modals/lib/context";
import { useRef } from "react";
import { Entity, EntityDefinition } from "../../Entity";
import { ValuesObject } from "../../client";
import { useModal } from "../Core";
import { GridSelectionMode } from "../Grid";
import { LookupForm } from "../Lookup";

export interface useLookupFormType {
    open: ({ entity, viewName, onOK, onCancel, modalProps, search }: ModalLookupOptions) => void;
}

export interface ModalLookupOptions {
    entity: Entity<EntityDefinition>,
    viewName: string,
    onOK: (selectedKeys: ValuesObject[]) => void,
    onCancel?: () => void,
    onClosed?: () => void,
    modalProps?: ModalSettings,
    selectionMode?: GridSelectionMode,
    search?: string[],
    selectLabel?: string,
    parentKeys?: ValuesObject,
    showActions?: boolean,
}

export const UseLookupFormDefaultProps: Partial<ModalLookupOptions> = {
    modalProps: { size: 'xl' },
    selectLabel: "Select",
    showActions: false
}
export function useLookupForm() {
    const modals = useModal();
    const buttonResult = useRef<'OK' | 'Cancel' | 'Quit'>('Quit');

    const open = async (props: ModalLookupOptions) => {
        const {
            entity, viewName, onOK, onCancel, modalProps, selectionMode, search, selectLabel, onClosed,
            parentKeys, showActions
        } = { ...UseLookupFormDefaultProps, ...props };

        buttonResult.current = 'Quit';

        const handleOK = async (selectedKeys: ValuesObject[]) => {
            buttonResult.current = 'OK';
            await modals.close();
            if (onOK) {
                await onOK(selectedKeys);
            }
        };

        const handleCancel = async () => {
            buttonResult.current = 'Cancel';
            await modals.close();
            if (onCancel) {
                onCancel();
            }
        };

        const handleClosed = async () => {
            if (buttonResult.current === 'Quit') {
                if (onCancel) {
                    onCancel();
                }
            }
            if (onClosed) onClosed();
        }

        await modals.open(
            {
                content: <LookupForm
                    dataGridProps={{
                        entity: entity,
                        parentKeys: parentKeys,
                        viewName: viewName,
                        limit: "10000",
                        refreshOnInit: true,
                        selectionMode: (selectionMode) ? selectionMode : 'multi',
                        search: search,
                        showActions: showActions
                    }}
                    onOK={async (selectedKeys: ValuesObject[]) => await handleOK(selectedKeys)}
                    onCancel={() => handleCancel()}
                />,
                modalProps: {
                    ...modalProps,
                    trapFocus: true,
                    returnFocus: true,
                    title: <Text fw="700">{selectLabel} {entity.Title}</Text>,
                },
                onClosed: async () => {
                    await handleClosed();
                }

            });

    };

    return open;

}