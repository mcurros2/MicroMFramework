import { useCallback } from "react";
import { Value } from "../../client";
import { EntityColumn } from "../../Entity";
import { UseEntityFormReturnType } from "../Form";

export interface useTextTransformProps {
    entityForm: UseEntityFormReturnType,
    column: EntityColumn<Value>,
    transform?: "uppercase" | "lowercase" | "capitalize" | "titlecase";
    autoTrim?: boolean
}

export function useTextTransform(props: useTextTransformProps) {

    const { entityForm, column, autoTrim, transform } = props;

    const handleTransform = useCallback((text: string) => {
        const value = autoTrim ? text.trim() : text;
        let transformedValue = value;

        if (transform) {
            switch (transform) {
                case "uppercase":
                    transformedValue = value.toUpperCase();
                    break;
                case "lowercase":
                    transformedValue = value.toLowerCase();
                    break;
                case "capitalize":
                    transformedValue = value.charAt(0).toUpperCase() + value.slice(1);
                    break;
                case "titlecase":
                    transformedValue = value.split(' ').map((word) => word.charAt(0).toUpperCase() + word.slice(1)).join(' ');
            }
        }

        if (autoTrim || transform) {
            entityForm.form.setFieldValue(column.name, transformedValue);
        }

        return transformedValue;

    }, [autoTrim, column.name, entityForm.form, transform]);

    return handleTransform;

}
