import { Group, NumberInput, NumberInputProps, useComponentDefaultProps } from "@mantine/core";
import { forwardRef, ReactNode } from "react";
import { Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { MicroMWidthSizes } from "./types";


export interface NumberFieldProps extends Omit<NumberInputProps, 'autoFocus'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean,
    maxWidth?: keyof typeof MicroMWidthSizes,
    minWidth?: keyof typeof MicroMWidthSizes,
}

const defaultProps: Partial<NumberFieldProps> = {
    validationContainer: Group
}

// MMC: This component in mantine has a bug: if you set setFieldValue and the field has autofocus, the value displayed is not set until onBlur.

export const NumberField = forwardRef<HTMLInputElement, NumberFieldProps>(function NumberField(props: NumberFieldProps, ref) {

    const {
        column, entityForm, validate, validationContainer, required, requiredMessage, readOnly, label,
        placeholder, description, withAsterisk, autoFocus, maxWidth, minWidth, maw, miw, precision, ...others
    } = useComponentDefaultProps('NumberField', defaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    const { formMode, status } = entityForm;
    const add_autofocus = formMode === 'add' ? true : undefined;
    const edit_autofocus = status.loading === false && formMode !== 'add' ? true : undefined;
    
    const resolved_maxWidth = maxWidth !== 'auto' && maxWidth !== undefined ? MicroMWidthSizes[maxWidth!] : undefined;
    const resolved_minWidth = minWidth !== 'auto' && minWidth !== undefined ? MicroMWidthSizes[minWidth!] : undefined;

    return (
        <NumberInput
            {...others}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            readOnly={entityForm.formMode === 'view' ? true : readOnly}
            data-autofocus={autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus}
            autoFocus={autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus}
            precision={precision ?? column.scale}
            maw={maw ?? resolved_maxWidth}
            miw={miw ?? resolved_minWidth}
            {...entityForm.form.getInputProps(column.name)}

            // FIX for mantine NumberInput not supporting null
            value={entityForm.form.values[column.name] === null ? '' : entityForm.form.values[column.name] as number}

            ref={ref}
        />
    )

});