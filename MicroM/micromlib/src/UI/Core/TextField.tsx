import { Group, px, TextInput, TextInputProps, useComponentDefaultProps } from "@mantine/core";
import { forwardRef, ReactNode, useCallback } from "react";
import { Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { MicroMWidthSizes } from "./types";
import { useTextTransform, useTextTransformProps } from "./useTextTransform";

export interface TextFieldProps extends Omit<TextInputProps, 'autoFocus'>, Omit<useTextTransformProps, 'entityForm' | 'column'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean,
    autoMaxWidth?: { columnLenghtLessThanOrEqual: number, maxWidth: string },
    maxWidth?: keyof typeof MicroMWidthSizes,
    minWidth?: keyof typeof MicroMWidthSizes,
}

const defaultProps: Partial<TextFieldProps> = {
    validationContainer: Group,
    autoTrim: true,
    autoMaxWidth: { columnLenghtLessThanOrEqual: 20, maxWidth: '20rem' },
}

export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(function TextField(props: TextFieldProps, ref) {

    const {
        column, entityForm, validate, validationContainer, maw, miw, required, requiredMessage, maxLength, readOnly, label,
        placeholder, description, withAsterisk, autoFocus, onBlur, onChange, onFocus, transform, autoTrim, iconWidth,
        autoMaxWidth, maxWidth, minWidth, ...others
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

    const { formMode, status } = entityForm;
    const add_autofocus = formMode === 'add' ? true : undefined;
    const edit_autofocus = status.loading === false && formMode !== 'add' ? true : undefined;

    const readonly_condition = readOnly === undefined ? column.hasFlag(EntityColumnFlags.autoNum) || (entityForm.formMode !== 'add' && column.hasFlag(EntityColumnFlags.pk)) : readOnly;

    const resolved_maw = maw ?? (maxWidth !== 'auto' && maxWidth !== undefined) ? MicroMWidthSizes[maxWidth!] : undefined
    const resolved_miw = miw ?? (minWidth !== 'auto' && minWidth !== undefined) ? MicroMWidthSizes[minWidth!] : undefined;

    return (
        <TextInput
            {...others}
            {...mantineProps}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            maw={resolved_maw ?? ((column.length <= autoMaxWidth!.columnLenghtLessThanOrEqual) ? autoMaxWidth!.maxWidth : undefined)}
            miw={resolved_miw}
            maxLength={maxLength ?? (column.length || undefined)}
            readOnly={entityForm.formMode === 'view' ? true : readonly_condition}
            data-autofocus={autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus}
            autoFocus={autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus}
            onBlur={handleOnBlur}
            onChange={handleOnChange}
            onFocus={handleOnFocus}
            iconWidth={iconWidth && typeof iconWidth === 'string' ? px(iconWidth) : iconWidth}
            ref={ref}
        />
    )

});