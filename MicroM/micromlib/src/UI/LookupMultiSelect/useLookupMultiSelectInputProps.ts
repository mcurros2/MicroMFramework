import { SelectItem } from "@mantine/core";
import { useCallback, useEffect, useMemo } from "react";
import { Value } from "../../client";
import { EntityColumn } from "../../Entity";
import { UseEntityFormReturnType } from "../Form";

export interface UseLookupMultiSelectInputPropsOptions {
    entityForm: UseEntityFormReturnType,
    column: EntityColumn<Value>,
    selectData: SelectItem[],
}

export function useLookupMultiSelectInputProps(props: UseLookupMultiSelectInputPropsOptions) {
    const { entityForm, column, selectData } = props;

    const bindingColumnValue = entityForm.form.values[column.name];

    const resolvedValues = useMemo(() => {
        const values = Array.isArray(bindingColumnValue)
            ? bindingColumnValue
            : (bindingColumnValue === null || bindingColumnValue === undefined || bindingColumnValue === '')
                ? []
                : [bindingColumnValue];

        return values.map((value) => {
            const stringValue = String(value);
            return selectData.find(
                item =>
                    typeof item.value === 'string' &&
                    item.value.localeCompare(stringValue, undefined, { sensitivity: 'base' }) === 0
            )?.value ?? stringValue;
        });
    }, [bindingColumnValue, selectData]);

    const { onChange: mantineOnChange, value: _mantineValue, ...inputProps } = entityForm.form.getInputProps(column.name);

    const handleMultiSelectChange = useCallback((nextValues: string[]) => {
        const normalizedValues = (nextValues ?? []).map((value) =>
            selectData.find(
                item =>
                    typeof item.value === 'string' &&
                    item.value.localeCompare(value, undefined, { sensitivity: 'base' }) === 0
            )?.value ?? value
        );

        mantineOnChange(normalizedValues);
    }, [mantineOnChange, selectData]);

    useEffect(() => {
        if (!column.isArray) return;

        // Mantiene el comportamiento previo para casos de mount/unmount antes de cargar data
        if (column.value && selectData.length === 0) return;

        const descriptions = resolvedValues.map((value) => {
            const index = selectData.findIndex((item) => item.value === value);
            return index >= 0 ? selectData[index].label : '';
        });

        column.valueDescription = descriptions.join(', ');
    }, [column, resolvedValues, selectData]);

    return {
        ...inputProps,
        value: resolvedValues,
        onChange: handleMultiSelectChange
    };
}