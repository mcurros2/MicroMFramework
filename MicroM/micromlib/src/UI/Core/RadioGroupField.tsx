import { Radio, useComponentDefaultProps } from "@mantine/core";
import { ComponentPropsWithoutRef, ReactNode, forwardRef, useCallback } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { Value } from "../../client/client.types";
import { UseEntityFormReturnType } from "../Form/useEntityForm";
import { useFieldConfiguration } from "../Form/useFieldConfiguration";

export interface RadioGroupFieldProps extends ComponentPropsWithoutRef<typeof Radio.Group> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,

    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: boolean,
    readOnly?: boolean,
    showDescription?: boolean,
}

export const RadioGroupFieldDefaultProps: Partial<RadioGroupFieldProps> = {
    showDescription: true
}

export const RadioGroupField = forwardRef<HTMLInputElement, RadioGroupFieldProps>(function RadioGroupField(props, ref) {
    const {
        entityForm, column, validationContainer, validate, required, requiredMessage, readOnly,
        onChange, onBlur, onFocus, children, withAsterisk, label, description, showDescription,
        ...others
    } = useComponentDefaultProps('RadioGroupField', RadioGroupFieldDefaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const { onBlur: mantineOnBlur, onChange: mantineOnChange, onFocus: mantineOnFocus, ...mantineProps } = entityForm.form.getInputProps(column.name);

    const handleOnBlur = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
        if (onBlur) onBlur(event);
        mantineOnBlur(event);

    }, [mantineOnBlur, onBlur]);

    const handleOnChange = useCallback((value: string) => {
        if (onChange) onChange(value);
        mantineOnChange(value);
    }, [mantineOnChange, onChange]);

    const handleOnFocus = useCallback((event: React.FocusEvent<HTMLDivElement>) => {
        if (onFocus) onFocus(event);
        mantineOnFocus(event);
    }, [mantineOnFocus, onFocus]);

    return (
        <Radio.Group
            {...others}
            {...mantineProps}

            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            description={showDescription ? (description ?? column.description) : ''}

            onBlur={handleOnBlur}
            onChange={handleOnChange}
            onFocus={handleOnFocus}
            ref={ref}
        >
            {children}
        </Radio.Group>
    )
});
