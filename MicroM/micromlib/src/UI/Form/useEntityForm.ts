import { useComponentDefaultProps } from "@mantine/core";
import { UseFormReturnType, useForm } from "@mantine/form";
import { LooseKeys } from "@mantine/form/lib/types";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { areValuesObjectsEqual, Entity, EntityColumn, EntityDefinition, isIn, setValues } from "../../Entity";
import { ValidationRule } from "../../Validation";
import { DBStatus, DBStatusResult, OperationStatus, SQLType, Value, ValuesObject, toDBStatusMicroMError, toMicroMError } from "../../client";
import { FormMode, FormOptions, useStateReturnType } from "../Core";
import { getMantineInitialValuesObject, getMantineValuesObject } from "./MantineFormHelpers";

export interface UseEntityFormOptions extends FormOptions<Entity<EntityDefinition>> {
    validateInputOnBlur?: boolean,
    validateInputOnChange?: boolean | LooseKeys<ValuesObject>[],
    forceDirty?: boolean,
    saveAndGetOverride?: (get_data_if_saved: boolean, override_values?: ValuesObject) => Promise<OperationStatus<DBStatusResult>>,
    noSaveOnSubmit?: boolean,
    bindedColumnNames?: string[],
    saveAndGetOnSubmit?: boolean,
    cancelGetOnUnmount?: boolean,
    cancelSaveOnUnmount?: boolean,
}

export type GetColumnInputPropsReturnType = {
    value: any,
    onChange: any,
    checked?: any,
    error?: any,
    onFocus?: any,
    onBlur?: any
}

export interface UseEntityFormReturnType {
    form: UseFormReturnType<ValuesObject>,
    status: OperationStatus<DBStatusResult | ValuesObject>,
    formMode: FormMode,
    handleCancel: () => Promise<void> | void,
    handleSubmit: (event?: React.FormEvent<HTMLFormElement>) => Promise<void>,
    performGetData: () => Promise<boolean>,
    saveAndGet: (get_data_if_saved: boolean, override_values?: ValuesObject) => Promise<OperationStatus<DBStatusResult>>,
    configureField: (column: EntityColumn<Value>, validation?: ValidationRule) => void,
    removeValidation: (column: EntityColumn<Value>) => void,
    notifyValidationErrorState: useStateReturnType<boolean>,
    showDescriptionState: useStateReturnType<boolean>,
    entity: Entity<EntityDefinition>,
    asyncErrors: Record<string, string>,
    setAsyncError: (column_name: string, error: string) => void,
    clearAsyncError: (column_name: string) => void,
    clearAllAsyncErrors: () => void,
    isFormValid: () => boolean,
    isFormFieldValid: (column_name: string) => boolean
}

export const UseEntityFormDefaultProps: Partial<UseEntityFormOptions> = {
    validateInputOnBlur: true,
    initialShowDescriptionInFields: true,
    cancelGetOnUnmount: true,
    cancelSaveOnUnmount: true
}
export function useEntityForm(props: UseEntityFormOptions): UseEntityFormReturnType {
    const {
        entity,
        initialFormMode,
        validateInputOnBlur,
        validateInputOnChange,
        onSaved,
        onCancel,
        getDataOnInit,
        forceDirty,
        initialShowDescriptionInFields,
        saveAndGetOverride,
        noSaveOnSubmit,
        bindedColumnNames,
        saveAndGetOnSubmit,
        cancelGetOnUnmount,
        cancelSaveOnUnmount,
        saveBeforeLocalNavigation,
        saveBeforeRemoteNavigation,
    } = useComponentDefaultProps('', UseEntityFormDefaultProps, props);

    const [status, setStatus] = useState<OperationStatus<DBStatusResult | ValuesObject>>({}); // Initial queryStatus is empty on purpose to not disable fields before data is loaded
    const notifyValidationErrorState = useState<boolean>(false);
    const [, setNotifyValidationError] = notifyValidationErrorState;
    const showDescriptionState = useState<boolean>(initialShowDescriptionInFields!);

    const [formMode, setFormMode] = useState(initialFormMode);

    const getAbortController = useRef<AbortController>(new AbortController);
    const saveAbortController = useRef<AbortController>(new AbortController);

    const validationObject = useRef<Record<string, ValidationRule>>({});
    const initialValues = useRef<ValuesObject>(getMantineInitialValuesObject(entity.def.columns, bindedColumnNames));
    const initialDirty = useRef<Record<string, boolean>>({});

    const form = useForm<ValuesObject>(
        {
            initialValues: initialValues.current,
            initialDirty: initialDirty.current,
            validateInputOnBlur: validateInputOnBlur,
            validateInputOnChange: validateInputOnChange,
            validate: validationObject.current
        }
    );

    // Async validation error state handling
    const asyncErrors = useRef<Record<string, string>>({});

    const setAsyncError = useCallback((column_name: string, error: string) => {
        asyncErrors.current[column_name] = error;
        form.setFieldError(column_name, error);
    }, [form]);

    const clearAsyncError = useCallback((column_name: string) => {
        delete asyncErrors.current[column_name];
        form.clearFieldError(column_name);
    }, [form]);

    const clearAllAsyncErrors = useCallback(() => {
        asyncErrors.current = {};
        form.clearErrors();
    }, [form]);

    // Custom form validation (async and sync)
    const isFormValid = useCallback(() => {
        if (Object.keys(asyncErrors.current).length > 0) return false;
        return form.isValid();
    }, [form]);

    const isFormFieldValid = useCallback((column_name: string) => {
        return !asyncErrors.current[column_name] && form.isValid(column_name);
    }, [form]);

    const lastGetValues = useRef<ValuesObject | undefined>();

    // Form Handlers
    const performGetData = useCallback(async () => {
        setStatus({ loading: true, operationType: "get" });

        try {
            // MMC: this also sets the underlying entity values...
            const ret = await entity.API.getData(getAbortController.current.signal);
            if (ret) {
                const new_values = getMantineValuesObject(form.values, entity.def.columns, true);
                form.setValues(new_values);
                lastGetValues.current = new_values;
                const new_status: OperationStatus<ValuesObject> = { loading: false, operationType: "get" }
                setStatus(new_status);
                form.resetDirty();
                form.resetTouched();
            }
            return ret;
        }
        catch (e: any) {
            if (e.name !== 'AbortError') {
                const new_status: OperationStatus<ValuesObject> = { loading: false, error: toMicroMError(e) };
                setStatus(new_status);
            }
        }
        return false;
    }, [entity.API, entity.def.columns, form])


    const saveAndGet = useCallback(async (get_data_if_saved: boolean = true, override_values?: ValuesObject) => {
        setValues(entity.def.columns, form.values, null, true);
        if (override_values) {
            setValues(entity.def.columns, override_values, null, true);
        }

        if (noSaveOnSubmit)
            return {
                loading: false, data: { Results: [{ Status: 0, Message: 'OK' }] }
            } as OperationStatus<DBStatusResult>;

        setStatus({ loading: true, operationType: "add" });

        // MMC: this also sets the underlying entity values if autonum...
        try {
            const data = formMode === 'add' ? await entity.API.addData(saveAbortController.current.signal) : await entity.API.editData(saveAbortController.current.signal);

            const new_status: OperationStatus<DBStatusResult> = { loading: false, data: data, operationType: formMode };
            setStatus(new_status);
            if (data.Failed !== true) {
                if (get_data_if_saved) await performGetData();
                if (formMode === "add") setFormMode('edit');
            }

            return new_status;
        }
        catch (e: any) {
            if (e.name !== 'AbortError') {
                const new_status: OperationStatus<DBStatusResult> = { error: e.Errors ? toDBStatusMicroMError(e.Errors as DBStatus[], formMode) : toMicroMError(e), operationType: formMode };
                setStatus(new_status);
                return new_status;
            }
            else {
                return { loading: false }
            }
        }
    }, [entity.API, entity.def.columns, form.values, formMode, performGetData]);

    const handleCancel = useCallback(async () => {
        getAbortController.current.abort();
        saveAbortController.current.abort();
        if (onCancel) await Promise.resolve(onCancel());
    }, [onCancel]);

    const handleSubmit = useCallback(async (event?: React.FormEvent<HTMLFormElement>) => {
        if (event) event.preventDefault();

        // Check if there are async errors
        if (Object.keys(asyncErrors.current).length > 0) {
            setNotifyValidationError(true);
            return;
        }

        // Ensure all fields are validated, this will reset async field errors
        const result = form.validate();

        if (result.hasErrors) {
            setNotifyValidationError(true);
        }
        else {
            let save_result: OperationStatus<DBStatusResult>;
            if (saveAndGetOverride) {
                try {
                    if (noSaveOnSubmit) {
                        save_result = {
                            loading: false, data: { Results: [{ Status: 0, Message: 'OK' }] }
                        } as OperationStatus<DBStatusResult>;
                    }
                    else {
                        setStatus({ loading: true, operationType: "add" });

                        const get_data_if_saved = saveAndGetOnSubmit || false;
                        save_result = await saveAndGetOverride(get_data_if_saved);
                        setStatus(save_result);
                        if (save_result.data?.Failed !== true) {
                            if (get_data_if_saved) await performGetData();
                            if (formMode === "add") setFormMode('edit');
                        }
                    }
                }
                catch (e: any) {
                    if (e.name !== 'AbortError') {
                        const new_status: OperationStatus<DBStatusResult> = { error: e.Errors ? toDBStatusMicroMError(e.Errors as DBStatus[], formMode) : toMicroMError(e), operationType: formMode };
                        setStatus(new_status);
                        save_result = new_status;
                    }
                    else {
                        save_result = { loading: false };
                    }
                }
            }
            else {
                save_result = await saveAndGet(saveAndGetOnSubmit || false);
            }

            if (save_result.error === undefined && save_result.data?.Failed !== true && onSaved) {
                // MMC: fix for mantine DateInput bug "invalid date"
                // check all form values for date columns and set them to null if invalid
                // The bug happens when closing the form with invalid date values, after save
                for (const c in entity.def.columns) {
                    if (isIn<SQLType>(entity.def.columns[c].type, 'date', 'datetime', 'datetime2', 'smalldatetime')) {
                        if (form.values[c]?.toString() === 'Invalid Date' || form.values[c]?.toString() === '') {
                            form.values[c] = null;
                            entity.def.columns[c].value = null;
                        }
                    }
                }
                onSaved(save_result);
            }

        }
    }, [form, onSaved, saveAndGet, saveAndGetOverride, setNotifyValidationError, saveAndGetOnSubmit]);

    // Validation, InitialValues, InitialDirty
    const addValidation = useCallback((column: EntityColumn<Value>, validation?: ValidationRule) => {
        if (validation) validationObject.current[column.name] = validation;
        else delete validationObject.current[column.name];

        if (initialFormMode === "add" || forceDirty) {
            initialDirty.current[column.name] = (column.value !== '' || column.value !== null || column.value !== undefined) ? true : false;
        }
        initialValues.current[column.name] = column.value ?? '';
    }, [forceDirty, initialFormMode]);

    const removeValidation = useCallback((column: EntityColumn<Value>) => {
        delete validationObject.current[column.name];
    }, []);

    // getDataOnInit
    useEffect(() => {
        const cancellation = getAbortController.current;

        if (getDataOnInit) {
            async function getData() {
                await performGetData();
            }
            getData();
        }

        return () => {
            if (getDataOnInit && cancelGetOnUnmount) {
                console.log("useEntityForm performGetData aborted");
                cancellation.abort("Effect cleanup");
            }
        }
    }, []);

    useEffect(() => {
        const cancellation = saveAbortController.current;
        return () => {
            if (cancelSaveOnUnmount) {
                console.log("useEntityForm Save aborted");
                cancellation.abort("Effect cleanup");
            }
        }
    }, []);

    // when the form mode is add, set the initial values
    useEffect(() => {
        if (initialFormMode === "add") {
            form.setValues(initialValues.current);
        }
    }, [initialFormMode]);

    // save before navigation
    useEffect(() => {
        const handleBeforeUnload = async () => {
            try {
                if (saveBeforeRemoteNavigation) {
                    form.validate();
                    if (!areValuesObjectsEqual(form.values, lastGetValues.current)) {
                        await saveAndGet();
                    }
                }
            } catch (ex) {
                console.error('SaveBeforeRemoteNavigation', ex);
            }
        };

        window.addEventListener("beforeunload", handleBeforeUnload);

        return () => {
            window.removeEventListener("beforeunload", handleBeforeUnload);
        };
    }, [form, saveAndGet, saveBeforeRemoteNavigation]);

    const prevLocationRef = useRef(window.location.hash);

    useEffect(() => {
        const handleLocalNavigation = async () => {
            try {
                if (saveBeforeLocalNavigation && form.isDirty() && (prevLocationRef.current !== window.location.hash)) {
                    form.validate();
                    if (!areValuesObjectsEqual(form.values, lastGetValues.current)) {
                        await saveAndGet();
                    }
                }
            } catch (ex) {
                console.error('SaveBeforeLocalNavigation', ex);
            }
        }

        window.addEventListener("hashchange", handleLocalNavigation);

        return () => {
            window.removeEventListener("hashchange", handleLocalNavigation);
        }

    }, [form, saveAndGet, saveBeforeLocalNavigation]);

    const result = useMemo(() => ({
        form: form,
        formMode: formMode,
        status: status,
        saveAndGet: saveAndGetOverride ?? saveAndGet,
        performGetData: performGetData,
        handleCancel: handleCancel,
        configureField: addValidation,
        removeValidation: removeValidation,
        handleSubmit: handleSubmit,
        notifyValidationErrorState: notifyValidationErrorState,
        showDescriptionState: showDescriptionState,
        entity: entity,
        asyncErrors: asyncErrors.current,
        setAsyncError: setAsyncError,
        clearAsyncError: clearAsyncError,
        clearAllAsyncErrors: clearAllAsyncErrors,
        isFormValid: isFormValid,
        isFormFieldValid: isFormFieldValid
    }), [addValidation, clearAllAsyncErrors, clearAsyncError, entity, form, formMode, handleCancel, handleSubmit, isFormFieldValid, isFormValid, notifyValidationErrorState, performGetData,
        removeValidation, saveAndGet, saveAndGetOverride, setAsyncError, showDescriptionState, status]);

    return result;
}