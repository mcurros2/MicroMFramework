import { useComponentDefaultProps } from "@mantine/core";
import { useForm, UseFormReturnType } from "@mantine/form";
import { LooseKeys } from "@mantine/form/lib/types";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { DBStatus, DBStatusResult, MicroMRequestOptions, OperationStatus, SQLType, toDBStatusMicroMError, toMicroMError, Value, ValuesObject } from "../../client";
import { areValuesObjectsEqual, Entity, EntityColumn, EntityColumnFlags, EntityDefinition, EntityFormActions, EntityFormClientAction, getValues, isIn, setValues } from "../../Entity";
import { ValidationRule } from "../../Validation";
import { FormMode, FormOptions, useStateReturnType, ValidateFormResult } from "../Core";
import { useModal } from "../Core/ModalsManager";
import { useConfirmNavigation } from "../Router/useConfirmNavigation";
import { getMantineInitialValuesObject, getMantineValuesObject } from "./MantineFormHelpers";
import { useValidateFormModals, ValidateFormLabelsDefaultProps } from "./useValidateFormModals";

export interface UseEntityFormOptions extends FormOptions<Entity<EntityDefinition>> {
    validateInputOnBlur?: boolean,
    validateInputOnChange?: boolean | LooseKeys<ValuesObject>[],
    forceDirty?: boolean,
    saveAndGetOverride?: (get_data_if_saved: boolean, override_values?: ValuesObject, requestOptions?: MicroMRequestOptions) => Promise<OperationStatus<DBStatusResult>>,
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

export type SilentSaveResult = 'saved' | 'unchanged' | 'invalid' | 'failed';

export interface UseEntityFormReturnType {
    form: UseFormReturnType<ValuesObject>,
    status: OperationStatus<DBStatusResult | ValuesObject>,
    formMode: FormMode,
    handleCancel: (silent?: boolean, element?: HTMLElement) => Promise<boolean>,
    handleSubmit: (event?: React.FormEvent<HTMLFormElement>, silent?: boolean, element?: HTMLElement, requestOptions?: MicroMRequestOptions) => Promise<boolean>,
    performGetData: () => Promise<boolean>,
    saveAndGet: (get_data_if_saved: boolean, override_values?: ValuesObject, requestOptions?: MicroMRequestOptions) => Promise<OperationStatus<DBStatusResult>>,
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
    isFormFieldValid: (column_name: string) => boolean,
    activeFormActions: EntityFormActions,
    silentSave: (requestOptions?: MicroMRequestOptions) => Promise<SilentSaveResult>,
}

export const UseEntityFormDefaultProps: Partial<UseEntityFormOptions> = {
    validateInputOnBlur: true,
    initialShowDescriptionInFields: true,
    cancelGetOnUnmount: true,
    cancelSaveOnUnmount: true,
    navigationProtection: 'confirm',
}

export function useEntityForm(props: UseEntityFormOptions): UseEntityFormReturnType {
    const {
        entity, initialFormMode, validateInputOnBlur, validateInputOnChange, onSaved, onCancel,
        getDataOnInit, forceDirty, initialShowDescriptionInFields, saveAndGetOverride, noSaveOnSubmit, bindedColumnNames,
        saveAndGetOnSubmit, cancelGetOnUnmount, cancelSaveOnUnmount, navigationProtection, validateForm,
    } = useComponentDefaultProps('', UseEntityFormDefaultProps, props);

    const { confirmWarning, showValidationError } = useValidateFormModals();
    const modal = useModal();

    const [status, setStatus] = useState<OperationStatus<DBStatusResult | ValuesObject>>({}); // Initial queryStatus is empty on purpose to not disable fields before data is loaded
    const notifyValidationErrorState = useState<boolean>(false);
    const [, setNotifyValidationError] = notifyValidationErrorState;
    const showDescriptionState = useState<boolean>(initialShowDescriptionInFields!);

    const [formMode, setFormMode] = useState(initialFormMode);

    const activeFormActions = useMemo<EntityFormActions>(() => {
        const allModesOverrides = entity.def.formActionOverrides.Allways;
        const modeOverrides = entity.def.formActionOverrides[formMode];

        return {
            OK: modeOverrides?.OK ?? allModesOverrides?.OK,
            Cancel: modeOverrides?.Cancel ?? allModesOverrides?.Cancel,
        };
    }, [entity.def.formActionOverrides, formMode]);

    const getAbortController = useRef<AbortController>(new AbortController);
    const saveAbortController = useRef<AbortController>(new AbortController);
    const getInFlightRef = useRef(false);
    const cancellableSaveInFlightRef = useRef(false);
    const preserveSilentSaveOnUnmountRef = useRef(false);
    const mountedRef = useRef(true);

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
        if (getAbortController.current.signal.aborted) getAbortController.current = new AbortController();

        const abortController = getAbortController.current;

        getInFlightRef.current = true;
        if (mountedRef.current) setStatus({ loading: true, operationType: "get" });

        try {
            // MMC: this also sets the underlying entity values...
            const ret = await entity.API.getData(abortController.signal);

            if (ret && mountedRef.current) {
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
            if (e.name !== 'AbortError' && mountedRef.current) {
                const new_status: OperationStatus<ValuesObject> = { loading: false, error: toMicroMError(e) };
                setStatus(new_status);
            }
        }
        finally {
            if (getAbortController.current === abortController) getInFlightRef.current = false;
        }
        return false;
    }, [entity.API, entity.def.columns, form])


    const saveAndGet = useCallback(async (get_data_if_saved: boolean = true, override_values?: ValuesObject, requestOptions?: MicroMRequestOptions) => {
        setValues(entity.def.columns, form.values, null, true);
        if (override_values) {
            setValues(entity.def.columns, override_values, null, true);
        }

        if (noSaveOnSubmit)
            return {
                loading: false, data: { Results: [{ Status: 0, Message: 'OK' }] }
            } as OperationStatus<DBStatusResult>;

        if (mountedRef.current) setStatus({ loading: true, operationType: formMode });

        // MMC: this also sets the underlying entity values if autonum...
        const isKeepaliveSave = requestOptions?.keepalive === true;
        if (!isKeepaliveSave && saveAbortController.current.signal.aborted) saveAbortController.current = new AbortController();

        const abortController = saveAbortController.current;
        if (!isKeepaliveSave) cancellableSaveInFlightRef.current = true;

        try {
            const abortSignal = isKeepaliveSave ? null : abortController.signal;

            const data = formMode === 'add'
                ? await entity.API.addData(abortSignal, undefined, undefined, requestOptions)
                : await entity.API.editData(abortSignal, undefined, requestOptions);

            const new_status: OperationStatus<DBStatusResult> = { loading: false, data: data, operationType: formMode };

            if (mountedRef.current) setStatus(new_status);

            if (data.Failed !== true) {
                if (get_data_if_saved && mountedRef.current) await performGetData();
                if (formMode === "add" && mountedRef.current) setFormMode('edit');
            }

            return new_status;
        }
        catch (e: any) {
            if (e.name !== 'AbortError') {
                const new_status: OperationStatus<DBStatusResult> = { error: e.Errors ? toDBStatusMicroMError(e.Errors as DBStatus[], formMode) : toMicroMError(e), operationType: formMode };
                if (mountedRef.current) setStatus(new_status);
                return new_status;
            }
            else {
                return { loading: false }
            }
        }
        finally {
            if (!isKeepaliveSave && saveAbortController.current === abortController) cancellableSaveInFlightRef.current = false;
        }
    }, [entity.API, entity.def.columns, form.values, formMode, noSaveOnSubmit, performGetData]);

    const validateBeforeSubmit = useCallback(async () => {
        // Check if there are async errors
        if (Object.keys(asyncErrors.current).length > 0) {
            setNotifyValidationError(true);
            return false;
        }

        // Ensure all fields are validated, this will reset async field errors
        const result = form.validate();

        if (result.hasErrors) {
            setNotifyValidationError(true);
            return false;
        }

        if (validateForm) {
            let validation: ValidateFormResult;
            try {
                validation = await Promise.resolve(validateForm(form.values));
            }
            catch (e) {
                console.error('validateForm callback failed', e);
                validation = { error: ValidateFormLabelsDefaultProps.unexpectedErrorLabel };
            }
            if (validation?.error) {
                await showValidationError(validation.error);
                return false;
            }
            else if (validation?.warning) {
                if (!await confirmWarning(validation.warning)) return false;
            }
            else if (validation?.success !== true) {
                // a malformed result (e.g. undefined from untyped JS) must not be treated as success
                console.error('validateForm returned an invalid result', validation);
                await showValidationError(ValidateFormLabelsDefaultProps.unexpectedErrorLabel);
                return false;
            }
        }

        return true;
    }, [confirmWarning, form, setNotifyValidationError, showValidationError, validateForm]);

    // Validation, InitialValues, InitialDirty
    const addValidation = useCallback((column: EntityColumn<Value>, validation?: ValidationRule) => {
        if (validation) validationObject.current[column.name] = validation;
        else delete validationObject.current[column.name];

        // In formMode == 'add' this is to force existing default values bound to input controls to be validated. If not mantine takes them as valid
        // and do not trigger validation
        if (initialFormMode === "add" || forceDirty) {
            if (column.value !== '' && column.value !== null && column.value !== undefined) initialDirty.current[column.name] = true;
        }

        initialValues.current[column.name] = column.value ?? '';
    }, [forceDirty, initialFormMode]);

    const removeValidation = useCallback((column: EntityColumn<Value>) => {
        delete validationObject.current[column.name];
    }, []);


    // MMC: silentSave handling
    const silentSaveInFlight = useRef<Promise<SilentSaveResult> | null>(null);

    const silentSave = useCallback((requestOptions?: MicroMRequestOptions): Promise<SilentSaveResult> => {
        if (requestOptions?.keepalive) preserveSilentSaveOnUnmountRef.current = true;
        if (silentSaveInFlight.current) return silentSaveInFlight.current;

        const savePromise = (async () => {
            try {
                if (formMode === 'view' || noSaveOnSubmit) return 'unchanged';

                const validationResult = form.validate();
                if (validationResult.hasErrors || Object.keys(asyncErrors.current).length > 0) {
                    if (mountedRef.current) setNotifyValidationError(true);
                    return 'invalid';
                }

                const savedValues = { ...form.values };
                if (areValuesObjectsEqual(savedValues, lastGetValues.current)) return 'unchanged';

                const getDataIfSaved = requestOptions?.keepalive !== true;
                let saveResult: OperationStatus<DBStatusResult>;

                if (saveAndGetOverride) {
                    if (mountedRef.current) setStatus({ loading: true, operationType: formMode });
                    saveResult = await saveAndGetOverride(getDataIfSaved, savedValues, requestOptions);
                    if (mountedRef.current) setStatus(saveResult);
                    if (mountedRef.current && saveResult.error === undefined && saveResult.data?.Failed !== true && formMode === "add") {
                        setFormMode('edit');
                    }
                }
                else {
                    saveResult = await saveAndGet(getDataIfSaved, savedValues, requestOptions);
                }

                if (saveResult.error !== undefined || saveResult.data?.Failed === true) return 'failed';

                if (!getDataIfSaved) {
                    const newValues = formMode === "add"
                        ? getMantineValuesObject(savedValues, entity.def.columns, true)
                        : savedValues;

                    lastGetValues.current = newValues;
                    if (mountedRef.current && areValuesObjectsEqual(form.values, savedValues)) {
                        if (!areValuesObjectsEqual(newValues, savedValues)) form.setValues(newValues);
                        form.resetDirty(newValues);
                    }
                }

                return 'saved';
            }
            catch (ex: unknown) {
                const errorObject = typeof ex === 'object' && ex !== null ? ex as { name?: string, Errors?: DBStatus[] } : undefined;
                if (errorObject?.name !== 'AbortError') {
                    if (mountedRef.current) setStatus({ error: errorObject?.Errors ? toDBStatusMicroMError(errorObject.Errors, formMode) : toMicroMError(ex), operationType: formMode });
                    console.error('SilentSave', ex);
                }
                return 'failed';
            }
        })();

        silentSaveInFlight.current = savePromise;
        void savePromise.finally(() => {
            if (silentSaveInFlight.current === savePromise) silentSaveInFlight.current = null;
            preserveSilentSaveOnUnmountRef.current = false;
        });

        return savePromise;
    }, [entity.def.columns, form, formMode, noSaveOnSubmit, saveAndGet, saveAndGetOverride, setNotifyValidationError]);

    const handleExecuteFormAction = useCallback(async (
        buttonName: 'OK' | 'Cancel',
        action: EntityFormClientAction,
        silent: boolean,
        element?: HTMLElement,
    ) => {
        if (!silent && buttonName === 'OK' && !await validateBeforeSubmit()) return false;

        // Keep the live entity synchronized so form actions can read current values.
        setValues(entity.def.columns, form.values, null, true);
        const selectedKeys = [getValues(entity.def.columns, { flags: EntityColumnFlags.pk, ignoreDefaults: false })];

        try {
            return await action.onClick({
                entity,
                modal,
                selectedKeys,
                element,
                silent,
                onClose: async (result?: boolean, actionStatus?: OperationStatus<DBStatusResult>) => {
                    if (result !== true) return false;

                    if (action.refreshOnClose && mountedRef.current) await performGetData();

                    if (buttonName === 'OK' && onSaved) {
                        await Promise.resolve(onSaved(actionStatus ?? {
                            loading: false,
                            operationType: formMode,
                        }));
                    }

                    return true;
                },
            });
        }
        catch (error) {
            if (!silent) throw error;
            console.error(`EntityForm ${buttonName} action failed silently`, error);
            return false;
        }
    }, [entity, form.values, formMode, modal, onSaved, performGetData, validateBeforeSubmit]);

    const handleSubmit = useCallback(async (
        event?: React.FormEvent<HTMLFormElement>,
        silent: boolean = false,
        element?: HTMLElement,
        requestOptions?: MicroMRequestOptions,
    ) => {
        event?.preventDefault();

        if (activeFormActions.OK) {
            return await handleExecuteFormAction('OK', activeFormActions.OK, silent, element);
        }

        if (silent) {
            const silentResult = await silentSave(requestOptions);
            return silentResult === 'saved' || silentResult === 'unchanged';
        }

        if (!await validateBeforeSubmit()) return false;

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

        const saved = save_result.error === undefined && save_result.data !== undefined && save_result.data.Failed !== true;

        if (saved && onSaved) {
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
            await Promise.resolve(onSaved(save_result));
        }

        return saved;
    }, [activeFormActions.OK, entity.def.columns, form.values, formMode, handleExecuteFormAction, noSaveOnSubmit, onSaved, performGetData, saveAndGet, saveAndGetOnSubmit, saveAndGetOverride, silentSave, validateBeforeSubmit]);

    const handleCancel = useCallback(async (silent: boolean = false, element?: HTMLElement) => {
        if (activeFormActions.Cancel) {
            return await handleExecuteFormAction('Cancel', activeFormActions.Cancel, silent, element);
        }

        getAbortController.current.abort();
        saveAbortController.current.abort();
        if (onCancel) await Promise.resolve(onCancel());
        return true;
    }, [activeFormActions.Cancel, handleExecuteFormAction, onCancel]);

    const hasUnsavedChanges = useCallback(() => formMode !== 'view'
        && ((navigationProtection === 'allways' && formMode === 'edit')
            || (form.isDirty()
                && !areValuesObjectsEqual(form.values, lastGetValues.current))), [form, formMode, navigationProtection]);

    useConfirmNavigation({
        mode: navigationProtection,
        hasUnsavedChanges,
        onSave: async (navigationType) => await handleSubmit(
            undefined,
            true,
            undefined,
            navigationType === 'remote' ? { keepalive: true } : undefined,
        ),
        onLeave: async () => await handleCancel(true),
    });

    useEffect(() => {
        mountedRef.current = true;
        return () => {
            mountedRef.current = false;
        };
    }, []);

    // getDataOnInit
    useEffect(() => {
        if (getDataOnInit) {
            async function getData() {
                await performGetData();
            }
            getData();
        }

        return () => {
            if (getDataOnInit && cancelGetOnUnmount && getInFlightRef.current && !getAbortController.current.signal.aborted) {
                console.log("useEntityForm performGetData aborted");
                getAbortController.current.abort("Effect cleanup");
            }
        }
    }, []);

    useEffect(() => {
        return () => {
            if (cancelSaveOnUnmount && cancellableSaveInFlightRef.current && !preserveSilentSaveOnUnmountRef.current && !saveAbortController.current.signal.aborted) {
                console.log("useEntityForm Save aborted");
                saveAbortController.current.abort("Effect cleanup");
            }
        }
    }, []);

    // when the form mode is add, set the initial values
    useEffect(() => {
        if (initialFormMode === "add") {
            form.setValues(initialValues.current);
        }
    }, [initialFormMode]);

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
        isFormFieldValid: isFormFieldValid,
        activeFormActions,
        silentSave,
    }), [activeFormActions, addValidation, clearAllAsyncErrors, clearAsyncError, entity, form, formMode, handleCancel, handleSubmit, isFormFieldValid,
        isFormValid, notifyValidationErrorState, performGetData, removeValidation, saveAndGet, saveAndGetOverride, setAsyncError, showDescriptionState, status, silentSave]);

    return result;
}
