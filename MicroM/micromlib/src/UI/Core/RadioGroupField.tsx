import { Radio, useComponentDefaultProps } from "@mantine/core";
import { ComponentPropsWithoutRef, forwardRef, ReactNode, useCallback } from "react";
import { Value } from "../../client/client.types";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { UseEntityFormReturnType } from "../Form/useEntityForm";
import { useFieldConfiguration } from "../Form/useFieldConfiguration";
import { FieldAutoFocus, FieldWidthProps, resolveFieldAutoFocus, resolveFieldReadOnly, resolveFieldWidths } from "./fieldUIHelpers";

export interface RadioGroupFieldProps extends Omit<ComponentPropsWithoutRef<typeof Radio.Group>, 'autoFocus'>, FieldWidthProps {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,

    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: FieldAutoFocus,
    readOnly?: boolean,
    disabled?: boolean,
    showDescription?: boolean,
}

export const RadioGroupFieldDefaultProps: Partial<RadioGroupFieldProps> = {
    showDescription: true
}

export const RadioGroupField = forwardRef<HTMLDivElement, RadioGroupFieldProps>(function RadioGroupField(props, ref) {
    const {
        entityForm, column, validationContainer, validate, required, requiredMessage, readOnly, disabled,
        onChange, onBlur, onFocus, children, withAsterisk, label, description, showDescription, autoFocus, maw, miw, maxWidth, minWidth,
        ...others
    } = useComponentDefaultProps('RadioGroupField', RadioGroupFieldDefaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const { onBlur: mantineOnBlur, onChange: mantineOnChange, onFocus: mantineOnFocus, ...mantineProps } = entityForm.form.getInputProps(column.name);

    const effectiveReadOnly = resolveFieldReadOnly(column, entityForm.formMode, readOnly);
    const resolvedAutoFocus = resolveFieldAutoFocus(autoFocus, entityForm.formMode, entityForm.status.loading);
    const widths = resolveFieldWidths({ maw, miw, maxWidth, minWidth });

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
            {...widths}
            autoFocus={resolvedAutoFocus}
            ref={ref}
        >
            <fieldset disabled={disabled || effectiveReadOnly} style={{ borderWidth: 0, margin: 0, padding: 0, minInlineSize: 'unset' }}>
                {children}
            </fieldset>
        </Radio.Group>
    )
});
