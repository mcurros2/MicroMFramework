import { UseFormReturnType } from "@mantine/form";
import { MutableRefObject, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { OperationStatus, toMicroMError, Value, ValuesObject } from "../../client";
import { composeCompoundKey, Entity, EntityDefinition, extractCompoundKeyValues, getOrderedCompoundKeyMappings, resolveCompoundKeyGroup, splitCompoundKey } from "../../Entity";
import * as cf from "../../Entity/ColumnsFunctions";
import { UseEntityFormReturnType } from "../Form";
import { LookupResultState, UseLookupReturnType } from "./useLookup";
import { useLookupForm } from "./useLookupForm";

interface UseCompoundLookupOptions {
    parentKeys?: ValuesObject;
    bindingColumns: string[];
    entityForm: UseEntityFormReturnType;
    entity: Entity<EntityDefinition>;
    lookupDefName: string;
    required?: boolean;
    inputRef: MutableRefObject<HTMLInputElement | null>;
    enableAdd?: boolean;
    enableEdit?: boolean;
    enableDelete?: boolean;
    enableView?: boolean;
    transform?: "uppercase" | "lowercase" | "capitalize" | "titlecase";
    autoTrim?: boolean;
    editLastLevelOnly?: boolean;
}

interface UseCompoundLookupReturnType extends UseLookupReturnType {
    keyPrefix: string;
}

interface DescribedCompoundValue {
    entity: Entity<EntityDefinition>;
    bindingColumnsKey: string;
    values: string[];
    description?: string;
}

function transformValue(value: string, autoTrim?: boolean, transform?: UseCompoundLookupOptions['transform']) {
    const transformed = autoTrim ? value.trim() : value;
    switch (transform) {
        case 'uppercase': return transformed.toUpperCase();
        case 'lowercase': return transformed.toLowerCase();
        case 'capitalize': return transformed.charAt(0).toUpperCase() + transformed.slice(1);
        case 'titlecase': return transformed.split(' ').map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(' ');
        default: return transformed;
    }
}

export function useCompoundLookup({
    entityForm, entity, lookupDefName, bindingColumns, parentKeys, required, inputRef,
    enableAdd, enableEdit, enableDelete, enableView, transform, autoTrim, editLastLevelOnly = false,
}: UseCompoundLookupOptions): UseCompoundLookupReturnType {
    const lookupForm = useLookupForm();
    const [status, setStatus] = useState<OperationStatus<ValuesObject>>({});
    const [lookupResult, setLookupResult] = useState<LookupResultState>();
    const [rawValue, setRawValue] = useState('');
    const [lastLevelValue, setLastLevelValue] = useState('');
    const dirty = useRef(false);
    const looking = useRef(false);
    const lastValidLookup = useRef<{ values: string[], description: string }>();

    const synchronization = useRef<{ initialized: boolean, signature: string, prefixSignature: string, loading?: boolean }>({
        initialized: false,
        signature: '',
        prefixSignature: '',
    });

    const significantColumn = bindingColumns[bindingColumns.length - 1];
    const bindingColumnsKey = bindingColumns.join('\u0000');

    const captureDescribedValue = (): DescribedCompoundValue => ({
        entity,
        bindingColumnsKey,
        values: bindingColumns.map(column => entity.def.columns[column].value?.toString() ?? ''),
        description: entity.def.columns[significantColumn].valueDescription,
    });

    const describedValue = useRef<DescribedCompoundValue>(captureDescribedValue());

    const descriptionInitialization = useRef<{ addHandled: boolean, loading?: boolean }>({
        addHandled: false,
        loading: entityForm.status.loading,
    });

    const context = useMemo(() => {
        if (bindingColumns.length < 2) throw new Error(`Compound Lookup '${lookupDefName}' requires at least two bindingColumns.`);

        const lookupDef = entity.def.lookups[lookupDefName];
        if (!lookupDef) throw new Error(`Lookup definition '${lookupDefName}' was not found in entity '${entity.name}'.`);
        if (!lookupDef.compoundKeyGroupName) throw new Error(`Compound Lookup '${lookupDefName}' requires compoundKeyGroupName.`);

        const lookupEntity = lookupDef.entityConstructor(entity.API.client, parentKeys);
        const viewName = lookupDef.view ?? lookupEntity.def.standardView() ?? '';
        const view = lookupEntity.def.views[viewName];

        if (!view) throw new Error(`View '${viewName}' was not found for compound Lookup '${lookupDefName}'.`);

        const group = resolveCompoundKeyGroup(view, lookupDef.compoundKeyGroupName);
        const mappings = getOrderedCompoundKeyMappings(group);

        if (mappings.length !== bindingColumns.length) {
            throw new Error(`Compound Lookup '${lookupDefName}' defines ${mappings.length} group members but received ${bindingColumns.length} bindingColumns.`);
        }

        return { lookupDef, lookupEntity, viewName, group, mappings };
    }, [bindingColumns, entity, lookupDefName, parentKeys]);

    const bindingSignature = bindingColumns.map(column => entityForm.form.values[column]?.toString() ?? '').join('\u0000');
    const separator = context.group.keySeparator ?? '-';
    const prefixValues = bindingColumns.slice(0, -1).map(column => entityForm.form.values[column]?.toString() ?? '');
    const keyPrefix = editLastLevelOnly ? `${prefixValues.join(separator)}${separator}` : '';

    // This effect synchronizes the displayed compound value with authoritative form state.
    useEffect(() => {
        // Read the values here because CompoundLookup registers every binding column in an
        // earlier effect. Mantine keeps those registered initial values in the form object.
        const values = bindingColumns.map(column => entityForm.form.values[column]);

        const currentSignature = values.map(value => value?.toString() ?? '').join('\u0000');

        const currentPrefixValues = values.slice(0, -1).map(value => value?.toString() ?? '');

        const currentPrefixSignature = currentPrefixValues.join('\u0000');

        const getCompleted = synchronization.current.loading === true && entityForm.status.loading === false && entityForm.status.operationType === 'get';

        const bindingsChanged = synchronization.current.signature !== currentSignature;
        const prefixChanged = synchronization.current.prefixSignature !== currentPrefixSignature;

        if (!synchronization.current.initialized || getCompleted || (!dirty.current && bindingsChanged)) {
            dirty.current = false;
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setRawValue(composeCompoundKey(values, context.group));

            // eslint-disable-next-line react-hooks/set-state-in-effect
            setLastLevelValue(values[values.length - 1]?.toString() ?? '');

        } else if (editLastLevelOnly && prefixChanged) {
            const currentPrefix = `${currentPrefixValues.join(separator)}${separator}`;

            // eslint-disable-next-line react-hooks/set-state-in-effect
            setRawValue(`${currentPrefix}${lastLevelValue}`);
        }

        synchronization.current = {
            initialized: true,
            signature: currentSignature,
            prefixSignature: currentPrefixSignature,
            loading: entityForm.status.loading,
        };

    }, [bindingColumns, bindingColumnsKey, bindingSignature, context.group, editLastLevelOnly, entityForm.form.values, entityForm.status.loading, entityForm.status.operationType, lastLevelValue, separator]);

    const clearDescriptions = useCallback(() => {
        bindingColumns.forEach(columnName => entity.def.columns[columnName].valueDescription = undefined);
    }, [bindingColumns, entity.def.columns]);

    const setInvalid = useCallback((raw: string, errorDescription?: string) => {
        clearDescriptions();

        entityForm.form.setFieldError(significantColumn, errorDescription || true);

        setLookupResult({ columnName: significantColumn, key: raw, description: '', error: true, cancel: false, updateParentKeys: false, errorDescription });

        queueMicrotask(() => inputRef.current?.focus());

    }, [clearDescriptions, entityForm.form, inputRef, significantColumn]);

    const commit = useCallback((values: readonly Value[], raw: string, description: string) => {
        const normalizedValues = values.map(value => value?.toString() ?? '');

        bindingColumns.forEach((columnName, index) => {
            if (editLastLevelOnly && index !== bindingColumns.length - 1) return;
            entityForm.form.setFieldValue(columnName, values[index]);
            entity.def.columns[columnName].value = values[index];
            entity.def.columns[columnName].valueDescription = index === bindingColumns.length - 1 ? description : undefined;
        });

        dirty.current = false;
        setRawValue(raw);
        setLastLevelValue(values[values.length - 1]?.toString() ?? '');
        entityForm.form.setFieldError(significantColumn, null);

        lastValidLookup.current = { values: normalizedValues, description };
        describedValue.current = { entity, bindingColumnsKey, values: normalizedValues, description };

        setLookupResult({ columnName: significantColumn, key: raw, description, error: false, cancel: false, updateParentKeys: true });
    }, [bindingColumns, bindingColumnsKey, editLastLevelOnly, entity, entityForm.form, significantColumn]);

    const executeDescriptionLookup = useCallback(async (values: readonly Value[]) => {
        const normalizedValues = values.map(value => value?.toString() ?? '');
        const cached = lastValidLookup.current;

        if (cached && cached.values.length === normalizedValues.length && cached.values.every((value, index) => value === normalizedValues[index])) {
            setStatus({ data: { description: cached.description } });
            return { description: cached.description, errorDescription: undefined };
        }

        try {
            setStatus({ loading: true });
            cf.setValues(context.lookupEntity.def.columns, parentKeys, null, true);
            context.mappings.forEach(([columnName], index) => context.lookupEntity.def.columns[columnName].value = values[index]);
            const description = await context.lookupEntity.API.lookupData(null, null, context.lookupDef.proc);
            setStatus({ data: { description } });
            if (description) lastValidLookup.current = { values: normalizedValues, description };
            return { description, errorDescription: undefined };
        }
        catch (error) {
            const microMError = toMicroMError(error);
            setStatus({ error: microMError });
            return { description: '', errorDescription: microMError.message ?? microMError.statusMessage ?? 'Lookup failed.' };
        }

    }, [context, parentKeys]);

    const browse = useCallback(async (raw: string): Promise<void> => {
        const browseValues = editLastLevelOnly
            ? [...prefixValues, lastLevelValue]
            : splitCompoundKey(raw, context.group).values;

        context.mappings.forEach(([columnName], index) => context.lookupEntity.def.columns[columnName].value = browseValues[index] ?? '');

        await new Promise<void>(async resolve => {
            await lookupForm({
                entity: context.lookupEntity,
                parentKeys,
                selectionMode: 'single',
                search: raw ? [raw] : [],
                viewName: context.viewName,
                onOK: async selectedKeys => {
                    const values = selectedKeys.length ? extractCompoundKeyValues(selectedKeys[0], context.group) : undefined;
                    if (!values) {
                        setInvalid(raw, 'The selected record contains an incomplete compound key.');
                        resolve();
                        return;
                    }
                    if (editLastLevelOnly && values.slice(0, -1).some((value, index) => value?.toString() !== prefixValues[index])) {
                        setInvalid(raw, 'The selected record does not match the fixed compound key prefix.');
                        resolve();
                        return;
                    }
                    const lookup = await executeDescriptionLookup(values);
                    if (lookup.errorDescription) setInvalid(raw, lookup.errorDescription);
                    else if (lookup.description) commit(values, composeCompoundKey(values, context.group), lookup.description);
                    else setInvalid(raw, 'The selected record could not be resolved.');
                    resolve();
                },
                onCancel: async () => {
                    setInvalid(raw);
                    setLookupResult({ columnName: significantColumn, key: raw, description: '', error: true, cancel: true, updateParentKeys: false });
                    resolve();
                },
                modalProps: { size: 'xl', trapFocus: true },
                enableAdd, enableEdit, enableDelete, enableView,
            });
        });

    }, [commit, context, editLastLevelOnly, enableAdd, enableDelete, enableEdit, enableView, executeDescriptionLookup, lastLevelValue, lookupForm, parentKeys, prefixValues, setInvalid, significantColumn]);

    const resolveReconciledDescription = useCallback(async (values: readonly Value[]) => {
        const normalizedValues = values.map(value => value?.toString() ?? '');
        const raw = composeCompoundKey(values, context.group);
        const split = splitCompoundKey(raw, context.group);

        if (!split.complete || normalizedValues.some(value => !value)) {
            clearDescriptions();
            setLookupResult(undefined);
            return;
        }

        const signature = normalizedValues.join('\u0000');
        const prior = describedValue.current;
        const priorMatches = !!prior.description
            && prior.entity === entity
            && prior.bindingColumnsKey === bindingColumnsKey
            && prior.values.length === normalizedValues.length
            && prior.values.every((value, index) => value === normalizedValues[index]);

        if (priorMatches) {
            const description = prior.description!;
            lastValidLookup.current = { values: normalizedValues, description };
            setStatus({ data: { description } });
            entityForm.form.setFieldError(significantColumn, null);
            setLookupResult({ columnName: significantColumn, key: raw, description, error: false, cancel: false, updateParentKeys: false });
            return;
        }

        clearDescriptions();
        const lookup = await executeDescriptionLookup(values);
        const currentSignature = bindingColumns.map(column => entityForm.form.values[column]?.toString() ?? '').join('\u0000');

        if (dirty.current || currentSignature !== signature) return;

        if (lookup.errorDescription) setInvalid(raw, lookup.errorDescription);
        else if (lookup.description) commit(values, raw, lookup.description);
        else await browse(raw);
    }, [bindingColumns, bindingColumnsKey, browse, clearDescriptions, commit, context.group, entity, entityForm.form, executeDescriptionLookup, setInvalid, significantColumn]);

    useEffect(() => {
        const getCompleted = descriptionInitialization.current.loading === true
            && entityForm.status.loading === false
            && entityForm.status.operationType === 'get';

        const initializeAdd = entityForm.formMode === 'add' && !descriptionInitialization.current.addHandled;

        descriptionInitialization.current.loading = entityForm.status.loading;
        if (initializeAdd) descriptionInitialization.current.addHandled = true;

        if (!initializeAdd && !getCompleted) return;

        const values = bindingColumns.map(column => entityForm.form.values[column]);
        looking.current = true;

        // Reconciliation intentionally initializes the controlled lookup result from form state.
        // eslint-disable-next-line react-hooks/set-state-in-effect
        void resolveReconciledDescription(values).finally(() => {
            looking.current = false;
        });

    }, [bindingColumns, bindingSignature, entityForm.form.values, entityForm.formMode, entityForm.status.loading, entityForm.status.operationType, resolveReconciledDescription]);

    const onBlur = useCallback(async (_bindingColumn: string, force = false, _event: React.FocusEvent | null = null) => {
        if (looking.current) return;

        looking.current = true;

        const transformedLastLevel = transformValue(lastLevelValue, autoTrim, transform);
        const transformed = editLastLevelOnly ? `${keyPrefix}${transformedLastLevel}` : transformValue(rawValue, autoTrim, transform);
        setRawValue(transformed);

        if (editLastLevelOnly) setLastLevelValue(transformedLastLevel);

        if (force) {
            await browse(transformed);
            looking.current = false;
            return;
        }

        if (editLastLevelOnly ? !transformedLastLevel : !transformed) {
            bindingColumns.forEach((columnName, index) => {
                if (editLastLevelOnly && index !== bindingColumns.length - 1) return;
                entityForm.form.setFieldValue(columnName, '');
                entity.def.columns[columnName].value = '';
            });
            clearDescriptions();
            entityForm.form.setFieldError(significantColumn, required ? true : null);
            setLookupResult({ columnName: significantColumn, key: '', description: '', error: required, cancel: false, updateParentKeys: true });
            dirty.current = false;
            looking.current = false;
            return;
        }

        const split = splitCompoundKey(transformed, context.group);

        if (!split.complete) {
            await browse(transformed);
        } else {
            const lookup = await executeDescriptionLookup(split.values);
            if (lookup.errorDescription) setInvalid(transformed, lookup.errorDescription);
            else if (lookup.description) commit(split.values, transformed, lookup.description);
            else await browse(transformed);
        }

        looking.current = false;

    }, [autoTrim, bindingColumns, browse, clearDescriptions, commit, context.group, editLastLevelOnly, entity.def.columns, entityForm.form, executeDescriptionLookup, keyPrefix, lastLevelValue, rawValue, required, setInvalid, significantColumn, transform]);

    const lookupInputProps = {
        ...entityForm.form.getInputProps(significantColumn),
        value: editLastLevelOnly ? lastLevelValue : rawValue,
        onChange: (event: React.ChangeEvent<HTMLInputElement>) => {
            dirty.current = true;
            const value = event.currentTarget.value;
            if (editLastLevelOnly) {
                setLastLevelValue(value);
                setRawValue(`${keyPrefix}${value}`);
            } else {
                setRawValue(value);
            }
            clearDescriptions();
            setLookupResult(undefined);
        },
    } as ReturnType<UseFormReturnType<ValuesObject>['getInputProps']>;

    return { status, lookupResult, lookupInputProps, onBlur, keyPrefix };
}
