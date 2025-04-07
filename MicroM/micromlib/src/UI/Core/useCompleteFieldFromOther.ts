import { UseFormReturnType } from "@mantine/form";
import { Value, ValuesObject } from "../../client";
import { EntityColumn } from "../../Entity";
import { useEffect, useState } from "react";

export interface UseCompleteFieldFromOtherProps {
    form: UseFormReturnType<ValuesObject>,
    originalColumn: EntityColumn<Value>,
    targetColumn: EntityColumn<Value>,
    onlyIfEmpty?: boolean,
    transformValue?: (value: Value) => Value
}

export function useCompleteFieldFromOther({ form, originalColumn, targetColumn, onlyIfEmpty, transformValue }: UseCompleteFieldFromOtherProps) {

    const [previousValue, setPreviousValue] = useState<Value>(form.values[originalColumn.name]);

    useEffect(() => {
        if (originalColumn.type !== targetColumn.type) {
            console.error(`useCompleteFieldFromOther: originalColumn.type !== targetColumn.type`);
            return;
        }
        if (form.values[originalColumn.name] !== previousValue) {
            // skip if onlyIfEmpty is true and targetColumn is not empty
            if (onlyIfEmpty && form.values[targetColumn.name]) return;

            setPreviousValue(form.values[originalColumn.name]);

            if (transformValue) {
                form.setFieldValue(targetColumn.name, transformValue(form.values[originalColumn.name]));
            }
            else {
                form.setFieldValue(targetColumn.name, form.values[originalColumn.name]);
            }
        }
    }, [form.values[originalColumn.name], form.values[targetColumn.name], previousValue])

}