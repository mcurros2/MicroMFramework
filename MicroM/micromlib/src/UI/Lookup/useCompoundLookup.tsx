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
    enableAdd, enableEdit, enableDelete, enableView, transform, autoTrim,
}: UseCompoundLookupOptions): UseLookupReturnType {
    const lookupForm = useLookupForm();
    const [status, setStatus] = useState<OperationStatus<ValuesObject>>({});
    const [lookupResult, setLookupResult] = useState<LookupResultState>();
    const [rawValue, setRawValue] = useState('');
    const dirty = useRef(false);
    const looking = useRef(false);
    const significantColumn = bindingColumns[bindingColumns.length - 1];
    const bindingColumnsKey = bindingColumns.join('\u0000');

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
    }, [bindingColumnsKey, entity, lookupDefName, parentKeys]);

    const bindingSignature = bindingColumns.map(column => entityForm.form.values[column]?.toString() ?? '').join('\u0000');
    useEffect(() => {
        if (!dirty.current || entityForm.status.loading || entityForm.status.operationType === 'get') {
            dirty.current = false;
            setRawValue(composeCompoundKey(bindingColumns.map(column => entityForm.form.values[column]), context.group));
        }
    }, [bindingSignature, bindingColumnsKey, context.group, entityForm.status.loading, entityForm.status.operationType]);

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
        bindingColumns.forEach((columnName, index) => {
            entityForm.form.setFieldValue(columnName, values[index]);
            entity.def.columns[columnName].value = values[index];
            entity.def.columns[columnName].valueDescription = index === bindingColumns.length - 1 ? description : undefined;
        });
        dirty.current = false;
        setRawValue(raw);
        entityForm.form.setFieldError(significantColumn, null);
        setLookupResult({ columnName: significantColumn, key: raw, description, error: false, cancel: false, updateParentKeys: true });
    }, [bindingColumns, entity.def.columns, entityForm.form, significantColumn]);

    const executeDescriptionLookup = useCallback(async (values: readonly Value[]) => {
        try {
            setStatus({ loading: true });
            cf.setValues(context.lookupEntity.def.columns, parentKeys, null, true);
            context.mappings.forEach(([columnName], index) => context.lookupEntity.def.columns[columnName].value = values[index]);
            const description = await context.lookupEntity.API.lookupData(null, null, context.lookupDef.proc);
            setStatus({ data: { description } });
            return { description, errorDescription: undefined };
        } catch (error) {
            const microMError = toMicroMError(error);
            setStatus({ error: microMError });
            return { description: '', errorDescription: microMError.message ?? microMError.statusMessage ?? 'Lookup failed.' };
        }
    }, [context, parentKeys]);

    const browse = useCallback(async (raw: string): Promise<void> => {
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
    }, [commit, context, enableAdd, enableDelete, enableEdit, enableView, executeDescriptionLookup, lookupForm, parentKeys, setInvalid, significantColumn]);

    const onBlur = useCallback(async (_bindingColumn: string, force = false, _event: React.FocusEvent | null = null) => {
        if (looking.current) return;
        looking.current = true;
        const transformed = transformValue(rawValue, autoTrim, transform);
        setRawValue(transformed);

        if (!transformed) {
            bindingColumns.forEach(columnName => {
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
        if (force || !split.complete) {
            await browse(transformed);
        } else {
            const lookup = await executeDescriptionLookup(split.values);
            if (lookup.errorDescription) setInvalid(transformed, lookup.errorDescription);
            else if (lookup.description) commit(split.values, transformed, lookup.description);
            else await browse(transformed);
        }
        looking.current = false;
    }, [autoTrim, bindingColumns, browse, clearDescriptions, commit, context.group, entity.def.columns, entityForm.form, executeDescriptionLookup, rawValue, required, setInvalid, significantColumn, transform]);

    const lookupInputProps = {
        ...entityForm.form.getInputProps(significantColumn),
        value: rawValue,
        onChange: (event: React.ChangeEvent<HTMLInputElement>) => {
            dirty.current = true;
            setRawValue(event.currentTarget.value);
            clearDescriptions();
            setLookupResult(undefined);
        },
    } as ReturnType<UseFormReturnType<ValuesObject>['getInputProps']>;

    return { status, lookupResult, lookupInputProps, onBlur };
}
