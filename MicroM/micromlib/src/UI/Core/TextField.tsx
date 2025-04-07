import { Group, TextInput, TextInputProps, useComponentDefaultProps } from "@mantine/core";
import { ReactNode, forwardRef, useCallback } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { Value } from "../../client";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { useTextTransform, useTextTransformProps } from "./useTextTransform";

export interface TextFieldProps extends TextInputProps, Omit<useTextTransformProps, 'entityForm' | 'column'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: boolean,
}

const defaultProps: Partial<TextFieldProps> = {
    validationContainer: Group,
    autoTrim: true
}

export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(function TextField(props: TextFieldProps, ref) {

    const {
        column, entityForm, validate, validationContainer, maw, required, requiredMessage, maxLength, readOnly, label,
        placeholder, description, withAsterisk, autoFocus, onBlur, onChange, onFocus, transform, autoTrim,
        ...others
    } = useComponentDefaultProps('TextField', defaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    // MMC: disabled when loading is done at the form level in EntityForm and fieldset
    //data-autofocus={autoFocus}  disabled={(disableOnLoading) ? loading : disabled}

    const { onBlur: mantineOnBlur, onChange: mantineOnChange, onFocus: mantineOnFocus, ...mantineProps } = entityForm.form.getInputProps(column.name);

    const textTransform = useTextTransform({ entityForm, column, transform, autoTrim });

    const handleOnBlur = useCallback((event: React.FocusEvent<HTMLInputElement>) => {

        textTransform(event.target.value);

        if (onBlur) onBlur(event);
        mantineOnBlur(event);

    }, [mantineOnBlur, onBlur, textTransform]);

    const handleOnChange = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
        if (onChange) onChange(event);
        mantineOnChange(event);
    }, [mantineOnChange, onChange]);

    const handleOnFocus = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
        if (onFocus) onFocus(event);
        mantineOnFocus(event);
    }, [mantineOnFocus, onFocus]);

    return (
        <TextInput
            {...others}
            {...mantineProps}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            maw={maw ?? ((column.length <= 20) ? { maxWidth: '10rem' } : undefined)}
            maxLength={maxLength ?? (column.length || undefined)}
            readOnly={entityForm.formMode === 'view' ? true : readOnly}
            data-autofocus={autoFocus}
            autoFocus={autoFocus}
            onBlur={handleOnBlur}
            onChange={handleOnChange}
            onFocus={handleOnFocus}
            ref={ref}
        />
    )

});