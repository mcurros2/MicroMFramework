import { Group, Textarea, TextareaProps, useComponentDefaultProps } from "@mantine/core";
import { ReactNode, forwardRef, useCallback } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfigurationParms } from "../../Validation";
import { Value } from "../../client";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { useTextTransform, useTextTransformProps } from "./useTextTransform";

type TextAreaFieldAllowedValidators = 'regex' | 'length' | 'field' | 'required';
export type TextAreaFieldValidatorConfiguration = Partial<Record<TextAreaFieldAllowedValidators, ValidatorConfigurationParms>>;

export interface TextAreaFieldProps extends TextareaProps, Omit<useTextTransformProps, 'entityForm' | 'column' | 'onBlur' | 'mantineOnBlur'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: TextAreaFieldValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>,
    autofocus?: boolean
}

const defaultProps: Partial<TextAreaFieldProps> = {
    validationContainer: Group
}

export const TextAreaField = forwardRef<HTMLTextAreaElement, TextAreaFieldProps>(function TextAreaField(props: TextAreaFieldProps, ref) {

    const {
        column, entityForm, required, label, validate, requiredMessage, validationContainer, maxLength,
        placeholder, description, readOnly, withAsterisk, autofocus, transform, autoTrim, onBlur, onChange, onFocus, ...others
    } = useComponentDefaultProps('TextAreaField', defaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage });

    const { onBlur: mantineOnBlur, onChange: mantineOnChange, onFocus: mantineOnFocus, ...mantineProps } = entityForm.form.getInputProps(column.name);

    const textTransform = useTextTransform({ entityForm, column, transform, autoTrim });

    const handleOnBlur = useCallback((event: React.FocusEvent<HTMLTextAreaElement>) => {

        textTransform(event.target.value);

        if (onBlur) onBlur(event);
        mantineOnBlur(event);

    }, [mantineOnBlur, onBlur, textTransform]);

    const handleOnChange = useCallback((event: React.ChangeEvent<HTMLTextAreaElement>) => {
        if (onChange) onChange(event);
        mantineOnChange(event);
    }, [mantineOnChange, onChange]);

    const handleOnFocus = useCallback((event: React.FocusEvent<HTMLTextAreaElement>) => {
        if (onFocus) onFocus(event);
        mantineOnFocus(event);
    }, [mantineOnFocus, onFocus]);


    const [showDescription,] = entityForm.showDescriptionState;

    return (
        <Textarea
            {...others}
            {...mantineProps}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            readOnly={entityForm.formMode === 'view' ? true : readOnly}
            onBlur={handleOnBlur}
            onChange={handleOnChange}
            onFocus={handleOnFocus}
            ref={ref}
            data-autofocus={autofocus}
            maxLength={maxLength ?? (column.length || undefined)}
        />

    );
});