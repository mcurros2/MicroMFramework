import React, { useCallback, useEffect, useRef, useState } from "react";
import { Entity, EntityDefinition, EntityLookup } from "../../Entity";
import * as cf from "../../Entity/ColumnsFunctions";
import { OperationStatus, Value, ValuesObject, toMicroMError } from "../../client";
import { UseEntityFormReturnType } from "../Form";
import { useLookupForm } from "../Lookup";

export interface LookupResultState {
    columnName: string,
    key: Value,
    description?: string,
    error?: boolean,
    cancel: boolean,
    updateParentKeys: boolean,
    errorDescription?: string
}

export interface UseLookupOptions {
    parentKeys?: ValuesObject,
    column: string,
    entityForm: UseEntityFormReturnType,
    entity: Entity<EntityDefinition>,
    lookupDefName: string,
    required?: boolean,
    HTMLDescriptionRef: React.MutableRefObject<any>,
    enableAdd?: boolean,
    enableEdit?: boolean,
    enableDelete?: boolean,
    enableView?: boolean,
}

export interface UseLookupReturnType {
    status: OperationStatus<ValuesObject>,
    lookupResult?: LookupResultState,
    lookupInputProps: any,
    onBlur: (bindingColumn: string, force?: boolean, event?: React.FocusEvent | null) => void;
}

export const useLookup = ({
    entityForm, entity, lookupDefName, column, parentKeys, required, HTMLDescriptionRef,
    enableAdd, enableEdit, enableDelete, enableView
}: UseLookupOptions): UseLookupReturnType => {
    const [status, setStatus] = useState<OperationStatus<ValuesObject>>({});
    const [previousLookupResult, setPreviousLookupResult] = useState<LookupResultState>();
    const [lookupResult, setLookupResult] = useState<LookupResultState>();

    const lookupForm = useLookupForm();

    const lookupEntity = useRef<Entity<EntityDefinition>>();
    const viewName = useRef<string>('');
    const lookupDef = useRef<EntityLookup>();
    const isLooking = useRef<boolean>(false);
    const lastFocusedElement = useRef<Element>();


    const performLookup = useCallback((bindingColumn: string, keyValue: Value, force: boolean = false): Promise<LookupResultState> => {
        if (!lookupEntity.current) {
            return Promise.resolve({ columnName: bindingColumn, key: '', description: '', cancel: false, error: false, updateParentKeys: true });
        }
        const mappedKeyColumnName = lookupDef.current!.bindingColumnKey ?? bindingColumn;
        lookupEntity.current.def.columns[mappedKeyColumnName].value = keyValue;
        if (parentKeys) lookupEntity.current.parentKeys = parentKeys;

        return new Promise<LookupResultState>(async (resolve, reject) => {

            const doLookup = async () => {
                try {
                    setStatus({ loading: true });
                    // Set parentKeys
                    cf.setValues(lookupEntity.current!.def.columns, parentKeys, null, true);

                    const result = await lookupEntity.current!.API.lookupData();
                    const new_status = {
                        data: { key: keyValue, description: result }
                    } as OperationStatus<ValuesObject>;
                    setStatus(new_status);
                    return { description: result, status: new_status, errorDescription: '' };
                }
                catch (e: any) {
                    const new_status = { error: toMicroMError(e) } as OperationStatus<ValuesObject>;
                    setStatus(new_status);
                    const errorDescription = `${new_status.data?.status ? new_status.data?.status : ''} ${new_status.data?.message ? new_status.data?.message : ''} ${new_status.data?.statusMessage ? new_status.data.statusMessage : ''}`
                    return { description: '', status: new_status, errorDescription: errorDescription };
                }
            }

            const doBrowse = async (search?: string[]) => {
                const onOK = async (selectedKeys: ValuesObject[]) => {
                    //console.log(`OnOK force: ${force} isLooking ${isLooking.current}`);
                    if (selectedKeys.length > 0) {
                        // MMC: map the binding column using binding column key or the column name
                        const selectedKeyValue = selectedKeys[0][mappedKeyColumnName];
                        lookupEntity.current!.def.columns[mappedKeyColumnName].value = selectedKeyValue;
                        entityForm.form.setFieldValue(column, selectedKeyValue);

                        //lookupEntity.current!.def.columns[bindingColumn].value = selectedKeys[0][bindingColumn];
                        const result = await doLookup();
                        if (result.status.error) {
                            resolve({ columnName: bindingColumn, key: keyValue, description: '', cancel: false, error: true, updateParentKeys: true, errorDescription: result.errorDescription });
                        }
                        else {
                            resolve({ columnName: bindingColumn, key: selectedKeyValue, description: result.description, cancel: false, error: false, updateParentKeys: true });
                        }
                    } else {
                        resolve({ columnName: bindingColumn, key: keyValue, description: '', cancel: true, error: false, updateParentKeys: true });
                    }
                };

                const onCancel = async () => {
                    //console.log(`OnCancel force: ${force} isLooking ${isLooking.current}`);

                    resolve({ columnName: bindingColumn, key: keyValue, description: '', cancel: true, error: false, updateParentKeys: false });
                }

                await lookupForm({
                    entity: lookupEntity.current!,
                    parentKeys: parentKeys,
                    selectionMode: "single",
                    search: search,
                    viewName: viewName.current,
                    onOK: onOK,
                    onCancel: onCancel,
                    modalProps: { size: "xl", trapFocus: true },
                    enableAdd: enableAdd,
                    enableEdit: enableEdit,
                    enableDelete: enableDelete,
                    enableView: enableView,
                });
            }

            if (force) {
                await doBrowse([]);
            }
            else {
                const result = await doLookup();
                if (result.status.error) {
                    resolve({ columnName: bindingColumn, key: keyValue, description: '', cancel: false, error: true, updateParentKeys: true, errorDescription: result.errorDescription });
                }
                else {
                    if (result.description) {
                        resolve({ columnName: bindingColumn, key: keyValue, description: result.description, cancel: false, error: false, updateParentKeys: true });
                    }
                    else {
                        await doBrowse(keyValue ? [keyValue?.toString()] : []);
                    }
                }
            }

        });
    }, [column, entityForm.form, lookupForm, parentKeys, enableAdd, enableEdit, enableDelete, enableView]);

    const updateLookupType = useCallback((result: LookupResultState) => {
        if (!result.error && !result.cancel) {
            entityForm.form.setFieldError(result.columnName, null);
            entityForm.form.setFieldValue(result.columnName, result.key);

            setLookupResult(result);
            setPreviousLookupResult(result);
        } else if (result.cancel) {

            if (result.key !== previousLookupResult?.key) {
                setLookupResult(result);
                entityForm.form.setFieldError(result.columnName, true);
                setPreviousLookupResult(undefined);
            }
        }
        else {
            console.log(`Error`);
            entityForm.form.setFieldError(result.columnName, true);
            result.description = '';
            setLookupResult(result);
            if (result.updateParentKeys) {
                setPreviousLookupResult(result);
            }
        }
    }, [entityForm.form, previousLookupResult?.key]);

    const lookupInputProps = entityForm.form.getInputProps(column);
    const mantine_onblur = lookupInputProps.onBlur;

    const onBlur = useCallback(async (bindingColumn: string, force: boolean = false, event: React.FocusEvent | null = null, new_value: Value | undefined = undefined) => {
        if (isLooking.current === true) return;
        isLooking.current = true;

        if (event) lastFocusedElement.current = event.target;
        else lastFocusedElement.current = document.activeElement as Element;

        const keyValue = new_value ?? entityForm.form.values[bindingColumn];

        if (!keyValue) {
            updateLookupType({
                columnName: bindingColumn,
                key: '',
                description: '',
                error: required,
                cancel: false,
                updateParentKeys: previousLookupResult?.key !== keyValue && keyValue !== null
            });

        }
        if (force === false && ((previousLookupResult?.key === keyValue && previousLookupResult?.error === false) || !keyValue)) {
            isLooking.current = false;
            return;
        }

        mantine_onblur();

        const result = await performLookup(bindingColumn, keyValue, force);

        //console.log(`OnBlur isLooking after: force: ${force} isLooking ${isLooking.current}`);
        isLooking.current = false;

        updateLookupType(result);

        if (lastFocusedElement.current) {
            //console.log(`Ref ${HTMLDescriptionRef.current}`)
            if (HTMLDescriptionRef.current) (HTMLDescriptionRef.current as HTMLElement).focus({ preventScroll: false });
        }


    }, [HTMLDescriptionRef, entityForm.form.values, mantine_onblur, performLookup, previousLookupResult?.error, previousLookupResult?.key, required, updateLookupType]);

    useEffect(() => {
        // MMC: create the lookup entity 
        lookupDef.current = entity.def.lookups[lookupDefName];
        lookupEntity.current = lookupDef.current.entityConstructor(entity.API.client, parentKeys);

        const stdview = lookupEntity.current.def.standardView() ?? '';
        viewName.current = lookupDef.current.view ? lookupDef.current.view : stdview;

    }, [entity, lookupDefName, parentKeys]);

    // MMC: perform the lookup after getting the entity data
    useEffect(() => {
        const initialLookup = async () => {
            if ((entityForm.formMode === 'edit' || entityForm.formMode === 'view') && entityForm.status.operationType === 'get' && !entityForm.status.loading && entity.def.columns[column].value) {
                const result = await performLookup(column, entity.def.columns[column].value, false);
                updateLookupType(result);
            };
        }
        initialLookup();
    }, [column, entity.def.columns, entityForm.formMode, entityForm.status.loading, entityForm.status.operationType, parentKeys]);

    return {
        status,
        lookupResult,
        lookupInputProps,
        onBlur
    };
};
