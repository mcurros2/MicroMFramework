import { UseFormReturnType } from "@mantine/form";
import React, { useCallback, useEffect, useRef, useState } from "react";
import { OperationStatus, toMicroMError, Value, ValuesObject } from "../../client";
import { areValuesObjectsEqual, copyValuesObject, Entity, EntityColumnFlags, EntityDefinition, EntityLookup } from "../../Entity";
import * as cf from "../../Entity/ColumnsFunctions";
import { UseEntityFormReturnType } from "../Form";
import { useLookupForm } from "../Lookup";
import { resolveLookupKeyColumn } from "./resolveLookupKeyColumn";

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
    HTMLDescriptionRef: React.RefObject<HTMLElement>,
    enableAdd?: boolean,
    enableEdit?: boolean,
    enableDelete?: boolean,
    enableView?: boolean,
    transform?: (value: string) => string | void,
}

export interface UseLookupReturnType {
    status: OperationStatus<ValuesObject>,
    lookupResult?: LookupResultState,
    lookupInputProps: ReturnType<UseFormReturnType<ValuesObject>['getInputProps']>,
    onBlur: (bindingColumn: string, force?: boolean, event?: React.FocusEvent | null) => void;
}

interface CachedLookup {
    values: ValuesObject;
    description: string;
}

export const useLookup = ({
    entityForm, entity, lookupDefName, column, parentKeys, required, HTMLDescriptionRef,
    enableAdd, enableEdit, enableDelete, enableView, transform
}: UseLookupOptions): UseLookupReturnType => {

    const [status, setStatus] = useState<OperationStatus<ValuesObject>>({});
    const [previousLookupResult, setPreviousLookupResult] = useState<LookupResultState>();
    const [lookupResult, setLookupResult] = useState<LookupResultState>();

    const lookupForm = useLookupForm();

    const lookupEntity = useRef<Entity<EntityDefinition>>();
    const viewName = useRef<string>('');
    const lookupDef = useRef<EntityLookup>();
    const lookupKeyColumn = useRef<string>();
    const lookupConfigurationError = useRef<string>();
    const loggedConfigurationError = useRef<string>();
    const isLooking = useRef<boolean>(false);
    const lastFocusedElement = useRef<Element>();
    const lastValidLookup = useRef<CachedLookup>();


    const performLookup = useCallback((bindingColumn: string, keyValue: Value, force: boolean = false): Promise<LookupResultState> => {
        const currentLookupEntity = lookupEntity.current;
        const currentLookupDef = lookupDef.current;
        const currentViewName = viewName.current;

        if (!currentLookupEntity || !currentLookupDef) {
            return Promise.resolve({ columnName: bindingColumn, key: '', description: '', cancel: false, error: false, updateParentKeys: true });
        }

        const mappedKeyColumnName = lookupKeyColumn.current;
        if (!mappedKeyColumnName) {
            return Promise.resolve({
                columnName: bindingColumn,
                key: keyValue,
                description: '',
                cancel: false,
                error: true,
                updateParentKeys: false,
                errorDescription: lookupConfigurationError.current ?? `Lookup '${currentLookupDef.name}' key column is not initialized.`
            });
        }

        const prepareLookupValues = (lookupKey: Value) => {
            // Parent keys provide the lookup context, but the explicit typed or selected
            // key must win when both sources contain the binding column.
            cf.setValues(currentLookupEntity.def.columns, parentKeys, null, true, true);
            currentLookupEntity.def.columns[mappedKeyColumnName].value = lookupKey;

            return cf.getValuesObject(
                currentLookupEntity.def.columns,
                { flags: EntityColumnFlags.pk | EntityColumnFlags.fk, ignoreDefaults: false }
            );
        };

        prepareLookupValues(keyValue);
        if (parentKeys) currentLookupEntity.parentKeys = parentKeys;

        return new Promise<LookupResultState>(async (resolve) => {

            const doLookup = async (lookupKey: Value = keyValue) => {
                const requestValues = prepareLookupValues(lookupKey);
                const cached = lastValidLookup.current;
                if (cached && areValuesObjectsEqual(cached.values, requestValues)) {
                    const cachedStatus = {
                        data: { key: lookupKey, description: cached.description }
                    } as OperationStatus<ValuesObject>;
                    setStatus(cachedStatus);
                    return { description: cached.description, status: cachedStatus, errorDescription: '' };
                }
                try {
                    setStatus({ loading: true });

                    const result = await currentLookupEntity.API.lookupData(null, null, currentLookupDef.proc);
                    const new_status = {
                        data: { key: lookupKey, description: result }
                    } as OperationStatus<ValuesObject>;
                    setStatus(new_status);
                    if (result) {
                        lastValidLookup.current = {
                            values: copyValuesObject(requestValues),
                            description: result,
                        };
                    }
                    return { description: result, status: new_status, errorDescription: '' };
                }
                catch (e: unknown) {
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
                        if (!Object.prototype.hasOwnProperty.call(selectedKeys[0], mappedKeyColumnName)) {
                            const errorDescription = `Lookup '${currentLookupDef.name}' selection does not contain the inferred key column '${mappedKeyColumnName}'.`;
                            console.error(errorDescription);
                            resolve({ columnName: bindingColumn, key: keyValue, description: '', cancel: false, error: true, updateParentKeys: false, errorDescription });
                            return;
                        }

                        const selectedKeyValue = selectedKeys[0][mappedKeyColumnName];
                        entityForm.form.setFieldValue(column, selectedKeyValue);

                        //lookupEntity.current!.def.columns[bindingColumn].value = selectedKeys[0][bindingColumn];
                        const result = await doLookup(selectedKeyValue);
                        if (result.status.error) {
                            resolve({ columnName: bindingColumn, key: selectedKeyValue, description: '', cancel: false, error: true, updateParentKeys: true, errorDescription: result.errorDescription });
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
                    entity: currentLookupEntity,
                    parentKeys: parentKeys,
                    selectionMode: "single",
                    search: search,
                    viewName: currentViewName,
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
    }, [column, enableAdd, enableDelete, enableEdit, enableView, entityForm.form, lookupForm, parentKeys]);

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

    const lookupInputProps: ReturnType<UseFormReturnType<ValuesObject>['getInputProps']> = entityForm.form.getInputProps(column);
    const mantine_onchange = lookupInputProps.onChange;
    const mantine_onblur = lookupInputProps.onBlur;
    const bindingValue = entityForm.form.values[column];

    const lookupResultMatchesBinding = !lookupResult || areValuesObjectsEqual({ key: lookupResult.key }, { key: bindingValue });
    const resolvedLookupResult = lookupResultMatchesBinding ? lookupResult : undefined;

    useEffect(() => {
        if (lookupResultMatchesBinding) return;

        // The form can be changed outside this control (for example, when a hierarchy
        // parent clears its descendants). Clear the stored result after immediately
        // suppressing the stale description above.
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setLookupResult(undefined);
        setPreviousLookupResult(undefined);
    }, [lookupResultMatchesBinding]);

    const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setLookupResult(undefined);
        setPreviousLookupResult(undefined);
        mantine_onchange(event);
    };

    const onBlur = useCallback(async (bindingColumn: string, force: boolean = false, event: React.FocusEvent | null = null, new_value: Value | undefined = undefined) => {
        if (isLooking.current === true) return;
        isLooking.current = true;

        if (event) lastFocusedElement.current = event.target;
        else lastFocusedElement.current = document.activeElement as Element;

        const rawKeyValue = new_value ?? entityForm.form.values[bindingColumn];
        const transformedKeyValue = typeof rawKeyValue === 'string' ? transform?.(rawKeyValue) : undefined;
        const keyValue = transformedKeyValue ?? rawKeyValue;

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
            if (HTMLDescriptionRef.current) HTMLDescriptionRef.current.focus({ preventScroll: false });
        }


    }, [HTMLDescriptionRef, entityForm.form.values, mantine_onblur, performLookup, previousLookupResult?.error, previousLookupResult?.key, required, updateLookupType, transform]);

    useEffect(() => {
        // MMC: create the lookup entity 
        lookupDef.current = entity.def.lookups[lookupDefName];
        lookupEntity.current = lookupDef.current.entityConstructor(entity.API.client, parentKeys);

        const stdview = lookupEntity.current.def.standardView() ?? '';
        viewName.current = lookupDef.current.view ? lookupDef.current.view : stdview;

        const keyColumnResolution = resolveLookupKeyColumn(lookupDef.current, lookupEntity.current, viewName.current, column);
        lookupKeyColumn.current = keyColumnResolution.columnName;
        lookupConfigurationError.current = keyColumnResolution.error;

        if (keyColumnResolution.error && loggedConfigurationError.current !== keyColumnResolution.error) {
            console.error(keyColumnResolution.error);
            loggedConfigurationError.current = keyColumnResolution.error;
        } else if (!keyColumnResolution.error) {
            loggedConfigurationError.current = undefined;
        }

    }, [column, entity, lookupDefName, parentKeys]);

    // MMC: perform the lookup after getting the entity data
    useEffect(() => {
        const initialLookup = async () => {
            if ((entityForm.formMode === 'edit' || entityForm.formMode === 'view') && entityForm.status.operationType === 'get' && !entityForm.status.loading && entity.def.columns[column].value) {
                const result = await performLookup(column, entity.def.columns[column].value, false);
                updateLookupType(result);
            }
        }
        initialLookup();
    }, [column, entity.def.columns, entityForm.formMode, entityForm.status.loading, entityForm.status.operationType, parentKeys]);

    // MMC: perform initial lookup when add
    const initialAddLookup = useRef<boolean>(true);
    useEffect(() => {
        const initialLookup = async () => {
            if (entityForm.formMode === 'add' && initialAddLookup.current && entity.def.columns[column].value) {
                const result = await performLookup(column, entity.def.columns[column].value, false);
                updateLookupType(result);
                initialAddLookup.current = false;
            }
        }
        initialLookup();
    }, [entity.def.columns[column].value, entityForm.formMode]);

    return {
        status,
        lookupResult: resolvedLookupResult,
        lookupInputProps: { ...lookupInputProps, onChange: handleInputChange },
        onBlur
    };
};
