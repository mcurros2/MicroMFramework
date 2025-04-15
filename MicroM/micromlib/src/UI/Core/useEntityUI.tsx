import { Button, Group, Text } from "@mantine/core";
import { IconAlertCircle, IconAlertTriangle } from "@tabler/icons-react";
import { useCallback, useRef } from "react";
import { Entity, EntityClientAction, EntityColumnFlags, EntityDefinition, setValues } from "../../Entity";
import * as cf from "../../Entity/ColumnsFunctions";
import { DBStatusResult, OperationStatus, ValuesObject } from "../../client";
import { UseEntityFormReturnType } from "../Form";
import { useImportDataForm } from "../ImportData";
import { ConfirmAndExecutePanel } from "./ConfirmAndExecutePanel";
import { MicroMModalSize, useModal } from "./ModalsManager";
import { openEntityForm } from "./openEntityForm";
import { FormMode } from "./types";

export interface EntityUILabels {
    addLabel: string,
    editLabel: string
    deleteLabel: string,
    viewLabel: string,
    YouWillDeleteLabel: string,
    recordsLabel: string,
    recordLabel: string,
    AreYouSureLabel: string,
    warningLabel: string,
    YouMustSelectOneOrMoreRecordsToDelete: string,
    closeLabel: string,
    YouMustSelectOneOrMoreRecordsToExecuteAction: string,
    YouMustSelect: string,
    YouMustSelectBetween: string,
    YouMustSelectAtLeast: string,
    YouMustSelectMaximum: string,
}

export interface UseEntityUIProps {
    entity?: Entity<EntityDefinition>,
    parentKeys?: ValuesObject,
    modalFormSize?: MicroMModalSize,
    withModalFullscreenButton?: boolean,
    parentFormAPI?: UseEntityFormReturnType,
    saveFormBeforeAdd?: boolean,
    onModalSaved?: (new_status: OperationStatus<DBStatusResult | null>) => void,
    onModalCancelled?: () => void,
    onRecordsDeleted?: () => void,
    onActionRefreshOnClose?: () => void,
    labels?: EntityUILabels,
    onAddClick?: (gridAddClick: (element?: HTMLElement) => void, element?: HTMLElement) => void,
    onEditClick?: (keys: ValuesObject, element?: HTMLElement) => void,
    onDeleteClick?: (keys: ValuesObject[], element?: HTMLElement) => void,
    onActionExecuted?: (actionName: string, result?: boolean) => void,
    onModalClosed?: (cancelled?: boolean) => void,
}

export function useEntityUI(props: UseEntityUIProps) {
    const {
        entity, onModalCancelled, onModalSaved, modalFormSize, parentFormAPI, saveFormBeforeAdd, onModalClosed,
        parentKeys, labels, onRecordsDeleted, onActionRefreshOnClose, onAddClick, onEditClick, onDeleteClick, onActionExecuted,
        withModalFullscreenButton,
    } = props;

    const modals = useModal();

    const cancelled = useRef<boolean>();

    const handleModalSaved = useCallback(async (new_status: OperationStatus<DBStatusResult | null>) => {
        await modals.close();
        if (onModalSaved) await onModalSaved(new_status);
    }, [modals, onModalSaved]);

    const handleModalCancel = useCallback(async () => {
        cancelled.current = true;
        await modals.close();
        if (onModalCancelled) await onModalCancelled();
    }, [modals, onModalCancelled]);

    const handleModalClosed = useCallback(async () => {
        if (onModalClosed) await onModalClosed(cancelled.current);
    }, [onModalClosed]);

    const openForm = useCallback(async (entity: Entity<EntityDefinition>, initialFormMode: FormMode, title?: string, element?: HTMLElement, getDataOnInit?: boolean, onClosed?: () => Promise<void>) => {

        await openEntityForm({
            modals, title, element, handleModalSaved, handleModalCancel, modalFormSize, withModalFullscreenButton, handleModalClosed: async () => {
                await handleModalClosed();
                if (onClosed) await onClosed();
            },
            formProps: {
                entity,
                initialFormMode,
                getDataOnInit,
            }
        });

    }, [modals, handleModalSaved, handleModalCancel, modalFormSize, withModalFullscreenButton, handleModalClosed]);

    const importData = useImportDataForm({
        initialFormMode: 'add',
        modalFormSize: 'xl',
        handleModalCancel,
        handleModalSaved,
    });

    const handleSaveBeforeAdd = useCallback(async () => {
        //if (entity?.Form === null) return "notsaved";
        if (parentFormAPI && saveFormBeforeAdd && parentFormAPI?.formMode === "add") {
            const result = parentFormAPI.form.validate();
            if (!result.hasErrors) {
                const result = await parentFormAPI.saveAndGet(true);
                if (!result.error) {
                    return "saved";
                }
                else {
                    return "error";
                }
            }
            else {
                const [, setNotifyValidationError] = parentFormAPI.notifyValidationErrorState;
                setNotifyValidationError(true);
                return "error";
            }
        }

        return "notsaved";

    }, [parentFormAPI, saveFormBeforeAdd]);


    const handleImportDataClick = useCallback(async () => {
        if (!entity) return;

        const saveResult = await handleSaveBeforeAdd();
        if (saveResult !== 'error') {
            const importEntity = Entity.clone(entity);

            // Set parentKeys
            const mergedParentKeys = {
                ...cf.getValues(importEntity.def.columns, { flags: EntityColumnFlags.pk, ignoreDefaults: false }),
                ...Object.fromEntries(
                    Object.entries(parentKeys!).filter(([key, value]) => value != null && value !== "")
                )
            };

            importEntity.parentKeys = mergedParentKeys;

            await importData.openImportDataForm(importEntity);
        }
    }, [entity, handleSaveBeforeAdd, importData, parentKeys]);


    const internalAddClick = useCallback(async (element?: HTMLElement, onClosed?: (cancelled?: boolean) => void) => {
        if (entity?.Form === null) return;

        const addEntity = Entity.clone(entity!);
        // Set parentKeys
        cf.setValues(addEntity.def.columns, parentKeys, null, true, true);

        if (parentFormAPI) {
            cf.setValues(addEntity.def.columns, parentFormAPI.entity.def.columns, { flags: EntityColumnFlags.pk | EntityColumnFlags.fk, ignoreDefaults: false }, true, true);
        }

        await openForm(addEntity, "add", labels?.addLabel, element, undefined, async () => {
            if (onClosed) await onClosed(cancelled.current);
        });

    }, [entity, labels?.addLabel, openForm, parentFormAPI, parentKeys]);


    const handleAddClick = useCallback(async (element?: HTMLElement, onClosed?: (cancelled?: boolean) => void) => {
        if (!onAddClick && (entity?.Form === null || entity === undefined)) return;

        const resultSaveBeforeAdd = await handleSaveBeforeAdd();
        if (resultSaveBeforeAdd === "error") return;

        if (onAddClick) {
            await onAddClick(internalAddClick, element);
        }
        else {
            await internalAddClick(element, onClosed);
        }

    }, [onAddClick, entity, handleSaveBeforeAdd, internalAddClick]);


    const handleEditClick = useCallback(async (keys: ValuesObject, element?: HTMLElement, onClosed?: (cancelled?: boolean) => void) => {
        if (entity?.Form === null) return;
        if (keys) {
            // MMC: handleSaveBeforeAdd special case, if the parentform is in add mode and the form is not saved, we save it before editing
            const resultSaveBeforeAdd = await handleSaveBeforeAdd();
            if (resultSaveBeforeAdd === "error") return;

            const editEntity = Entity.clone(entity!);

            // Set parentKeys
            cf.setValues(editEntity.def.columns, parentKeys, null, true, true);

            if (resultSaveBeforeAdd === "saved") {
                cf.setValues(editEntity.def.columns, parentFormAPI!.entity.def.columns, { flags: EntityColumnFlags.pk | EntityColumnFlags.fk, ignoreDefaults: false }, true, true);
            }

            setValues(editEntity.def.columns, keys, null, true);

            await openForm(editEntity, "edit", labels?.editLabel, element, true, async () => {
                if (onClosed) await onClosed(cancelled.current);
            });
        }
    }, [entity, handleSaveBeforeAdd, parentKeys, openForm, labels?.editLabel, parentFormAPI]);

    const handleViewClick = useCallback(async (keys: ValuesObject, element?: HTMLElement, onClosed?: (cancelled?: boolean) => void) => {
        if (entity?.Form === null) return;
        if (keys) {
            const viewEntity = Entity.clone(entity!);

            // Set parentKeys
            cf.setValues(viewEntity.def.columns, parentKeys, null, true, true);

            setValues(viewEntity.def.columns, keys, null, true);

            await openForm(viewEntity, "view", labels?.viewLabel, element, true, async () => {
                if (onClosed) await onClosed(cancelled.current);
            });
        }
    }, [entity, parentKeys, openForm, labels?.viewLabel]);

    const handleDeleteClick = useCallback(async (keys: ValuesObject[], element?: HTMLElement) => {
        if (!entity) return;
        if (keys.length) {
            const abort_controller = new AbortController();

            await modals.open({
                modalProps: {
                    title: <Group><IconAlertTriangle size="1.5rem" stroke="1.5"></IconAlertTriangle> <Text fw="700">{labels?.warningLabel}</Text></Group>,
                },
                focusOnClosed: element,
                content:
                    <ConfirmAndExecutePanel
                        onOK={async () => {
                            const deleteEntity = Entity.clone(entity);
                            // Merge parentKeys with deleteEntity.parentKeys and remove keys named in keys
                            const mergedParentKeys = { ...cf.getValues(deleteEntity.def.columns, { flags: EntityColumnFlags.pk, ignoreDefaults: false }), ...parentKeys };

                            if (keys.length === 1) {
                                setValues(deleteEntity.def.columns, keys[0], null, true);

                                // Delete selected keys from mergedParentKeys
                                Object.keys(keys[0]).forEach(key => {
                                    delete mergedParentKeys[key];
                                });

                                const result = await deleteEntity.API.deleteData(undefined, abort_controller.signal, mergedParentKeys);

                                if (onRecordsDeleted) onRecordsDeleted();

                                await modals.close();
                                return result;
                            }
                            else {

                                // Delete selected keys from mergedParentKeys. We use the first key to get the keys names
                                Object.keys(keys[0]).forEach(key => {
                                    delete mergedParentKeys[key];
                                });

                                const result = await deleteEntity.API.deleteData(keys, abort_controller.signal, mergedParentKeys);

                                if (onRecordsDeleted) onRecordsDeleted();

                                await modals.close();
                                return result;
                            }
                        }}
                        onCancel={async () => { await abort_controller.abort(); await modals.close(); }}
                        content={<Text size="sm" mb="xs">{labels?.YouWillDeleteLabel} {keys.length} {labels?.recordsLabel}. {labels?.AreYouSureLabel}</Text>}
                        operation="delete"
                        okButtonText={labels?.deleteLabel}
                    />
            });
        }
        else {
            modals.open({
                modalProps: {
                    title: <Group><IconAlertCircle size="1.5rem" stroke="1.5" /> <Text fw="700">{labels?.warningLabel}</Text></Group>,
                },
                focusOnClosed: element,
                content:
                    <>
                        <Text size="sm" mb="xs">
                            {labels?.YouMustSelectOneOrMoreRecordsToDelete}
                        </Text>
                        <Group mt="xs" position="right">
                            <Button onClick={async () => await modals.close()}>{labels?.closeLabel}</Button>
                        </Group>
                    </>
            });
        }
    }, [entity, modals, labels, parentKeys]);

    const handleDeleteRecord = useCallback(async (keys: ValuesObject, element?: HTMLElement) => {
        await handleDeleteClick([keys], element);
    }, [handleDeleteClick]);


    const handleExecuteAction = useCallback(async (action: EntityClientAction, keys: ValuesObject[], element?: HTMLElement) => {
        if (!entity) return;
        if (action.dontRequireSelection ||
            (
                keys.length &&
                (action.minSelectedRecords === undefined || keys.length >= action.minSelectedRecords) &&
                (action.maxSelectedRecords === undefined || keys.length <= action.maxSelectedRecords)
            )
        ) {

            // MMC: if the action requires a form to be saved before executing, we do it here
            const resultSaveBeforeAdd = await handleSaveBeforeAdd();
            if (resultSaveBeforeAdd === "error") return;

            const execEntity = Entity.clone(entity);

            // Merge parentKeys with execEntity.parentKeys and remove keys named in keys
            const mergedParentKeys = { ...cf.getValues(execEntity.def.columns, { flags: EntityColumnFlags.pk, ignoreDefaults: false }), ...parentKeys };

            if (keys.length > 0) {
                // Delete selected keys from mergedParentKeys
                Object.keys(keys[0]).forEach(key => {
                    delete mergedParentKeys[key];
                });
            }

            execEntity.parentKeys = mergedParentKeys;

            if (resultSaveBeforeAdd === "saved") {
                // MMC: if the form was saved, we set the column values of the entity to the parentKeys of the form to get the autonum and keys
                cf.setValues(execEntity.def.columns, parentFormAPI!.entity.def.columns, { flags: EntityColumnFlags.pk | EntityColumnFlags.fk, ignoreDefaults: false }, true);
            }

            return await action.onClick({
                entity: execEntity, modal: modals, selectedKeys: keys, element: element, onClose: async (result?: boolean) => {
                    if (onActionExecuted) await onActionExecuted(action.name, result);
                    if (action.refreshOnClose && onActionRefreshOnClose) await onActionRefreshOnClose();
                    return Promise.resolve(result ?? false);
                }
            });

        }
        else {
            let message = labels?.YouMustSelectOneOrMoreRecordsToExecuteAction;

            if (action.minSelectedRecords !== undefined && action.maxSelectedRecords !== undefined) {
                if (action.minSelectedRecords === action.maxSelectedRecords) {
                    message = `${labels?.YouMustSelect} ${action.minSelectedRecords} ${action.minSelectedRecords === 1 ? labels?.recordLabel : labels?.recordsLabel}`;
                } else {
                    message = `${labels?.YouMustSelectBetween} ${action.minSelectedRecords} and ${action.maxSelectedRecords} ${labels?.recordsLabel}`;
                }
            } else if (action.minSelectedRecords !== undefined) {
                message = `${labels?.YouMustSelectAtLeast} ${action.minSelectedRecords} ${action.minSelectedRecords === 1 ? labels?.recordLabel : labels?.recordsLabel}`;
            } else if (action.maxSelectedRecords !== undefined) {
                message = `${labels?.YouMustSelectMaximum} ${action.maxSelectedRecords} ${action.maxSelectedRecords === 1 ? labels?.recordLabel : labels?.recordsLabel}`;
            }

            await modals.open({
                modalProps: {
                    title: <Group><IconAlertCircle size="1.5rem" stroke="1.5" /> <Text fw="700">{labels?.warningLabel}</Text></Group>,
                },
                focusOnClosed: element,
                content:
                    <>
                        <Text size="sm" mb="xs">
                            {message}
                        </Text>
                        <Group mt="xs" position="right">
                            <Button onClick={
                                async () => {
                                    await modals.close();
                                }
                            }>{labels?.closeLabel}</Button>
                        </Group>
                    </>
            });
        }
    }, [entity, parentFormAPI, handleSaveBeforeAdd, modals, labels]);

    return {
        handleAddClick,
        handleEditClick,
        handleViewClick,
        handleDeleteClick,
        handleExecuteAction,
        handleDeleteRecord,
        handleImportDataClick
    };

}