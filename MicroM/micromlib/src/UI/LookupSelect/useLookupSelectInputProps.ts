import { SelectItem } from "@mantine/core";
import { useCallback, useEffect, useMemo } from "react";
import { Value } from "../../client";
import { EntityColumn } from "../../Entity";
import { UseEntityFormReturnType } from "../Form";

export interface UseLookupSelectInputPropsOptions {
    entityForm: UseEntityFormReturnType,
    column: EntityColumn<Value>,
    selectData: SelectItem[],
}

export function useLookupSelectInputProps(props: UseLookupSelectInputPropsOptions) {
    const { entityForm, column, selectData } = props;

    const bindingColumnValue = entityForm.form.values[column.name];

    const resolvedBindingValue = useMemo(() => {
        if (bindingColumnValue !== undefined && bindingColumnValue !== null && typeof bindingColumnValue !== 'string') {
            console.error(`LookupSelect: Column.Value is not resolved to a string for column ${column.name}. This may cause unexpected behavior.`);
            return null;
        }

        if (typeof bindingColumnValue !== 'string') return null;

        return selectData.find(
            item =>
                typeof item.value === 'string' &&
                item.value.localeCompare(bindingColumnValue, undefined, { sensitivity: 'base' }) === 0
        )?.value ?? bindingColumnValue;
    }, [bindingColumnValue, column.name, selectData]);

    const { onChange: mantineOnChange, value: _mantineValue, ...inputProps } = entityForm.form.getInputProps(column.name);

    const handleSelectChange = useCallback((nextValue: string | null) => {
        if (typeof nextValue === 'string') {
            const matchedValue =
                selectData.find(
                    item =>
                        typeof item.value === 'string' &&
                        item.value.localeCompare(nextValue, undefined, { sensitivity: 'base' }) === 0
                )?.value ?? nextValue;

            mantineOnChange(matchedValue);
            return;
        }

        mantineOnChange(nextValue);
    }, [mantineOnChange, selectData]);

    useEffect(() => {
        if (resolvedBindingValue) {
            const index = selectData.findIndex((item) => item.value === resolvedBindingValue);
            column.valueDescription = index >= 0 ? selectData[index].label : '';
        } else {
            column.valueDescription = '';
        }
    }, [column, resolvedBindingValue, selectData]);

    return {
        ...inputProps,
        value: resolvedBindingValue,
        onChange: handleSelectChange
    };
}