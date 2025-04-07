import { useCallback } from "react";
import { EntityColumn } from "../../Entity";
import { Value } from "../../client";
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

        if (autoTrim || transform) {
            const value = autoTrim ? text.trim() : text;

            let transformedValue: string = value;

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

            entityForm.form.setFieldValue(column.name, transformedValue);
        }

    }, [autoTrim, column.name, entityForm.form, transform]);

    return handleTransform;

}