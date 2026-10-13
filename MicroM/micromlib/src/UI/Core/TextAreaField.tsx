import { Group, Textarea, TextareaProps, useComponentDefaultProps } from "@mantine/core";
import { forwardRef, ReactNode, useCallback } from "react";
import { Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfigurationParms } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { FieldWidthProps, resolveFieldAutoFocus, resolveFieldReadOnly, resolveFieldWidths } from "./fieldUIHelpers";
import { useTextTransform, useTextTransformProps } from "./useTextTransform";

type TextAreaFieldAllowedValidators = 'regex' | 'length' | 'field' | 'required';
export type TextAreaFieldValidatorConfiguration = Partial<Record<TextAreaFieldAllowedValidators, ValidatorConfigurationParms>>;

export interface TextAreaFieldProps extends Omit<TextareaProps, 'autoFocus'>, FieldWidthProps, Omit<useTextTransformProps, 'entityForm' | 'column' | 'onBlur' | 'mantineOnBlur'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: TextAreaFieldValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>,
    autoFocus?: 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean
}

const defaultProps: Partial<TextAreaFieldProps> = {
    validationContainer: Group
}

export const TextAreaField = forwardRef<HTMLTextAreaElement, TextAreaFieldProps>(function TextAreaField(props: TextAreaFieldProps, ref) {

    const {
        column, entityForm, required, label, validate, requiredMessage, validationContainer, maxLength,
        placeholder, description, readOnly, withAsterisk, autoFocus, transform, autoTrim, onBlur, onChange, onFocus, maw, miw, maxWidth, minWidth, ...others
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

    const resolvedAutoFocus = resolveFieldAutoFocus(autoFocus, entityForm.formMode, entityForm.status.loading);
    const effectiveReadOnly = resolveFieldReadOnly(column, entityForm.formMode, readOnly);
    const { maw: resolved_maw, miw: resolved_miw } = resolveFieldWidths({ maw, miw, maxWidth, minWidth });

    return (
        <Textarea
            {...others}
            {...mantineProps}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            readOnly={effectiveReadOnly}
            onBlur={handleOnBlur}
            onChange={handleOnChange}
            onFocus={handleOnFocus}
            maw={resolved_maw}
            miw={resolved_miw}
            ref={ref}
            data-autofocus={resolvedAutoFocus ? true : undefined}
            autoFocus={resolvedAutoFocus}
            maxLength={maxLength ?? (column.length || undefined)}
        />

    );
});