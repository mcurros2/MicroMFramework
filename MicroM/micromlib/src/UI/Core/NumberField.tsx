import { Group, NumberInput, NumberInputProps, useComponentDefaultProps } from "@mantine/core";
import { forwardRef, ReactNode } from "react";
import { Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { resolveFieldAutoFocus, resolveFieldReadOnly, resolveFieldWidths } from "./fieldUIHelpers";
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

export const NumberInputDefaultProps: Partial<NumberFieldProps> = {
    validationContainer: Group,
    maxWidth: "sm",
}

// MMC: This component in mantine has a bug: if you set setFieldValue and the field has autofocus, the value displayed is not set until onBlur.

export const NumberField = forwardRef<HTMLInputElement, NumberFieldProps>(function NumberField(props: NumberFieldProps, ref) {

    const {
        column, entityForm, validate, validationContainer, required, requiredMessage, readOnly, label,
        placeholder, description, withAsterisk, autoFocus, maxWidth, minWidth, maw, miw, precision, ...others
    } = useComponentDefaultProps('NumberField', NumberInputDefaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;


    

    const resolvedAutoFocus = resolveFieldAutoFocus(autoFocus, entityForm.formMode, entityForm.status.loading);
    const effectiveReadOnly = resolveFieldReadOnly(column, entityForm.formMode, readOnly);
    const { maw: resolved_maw, miw: resolved_miw } = resolveFieldWidths({ maw, miw, maxWidth, minWidth });

    return (
        <NumberInput
            {...others}
            {...entityForm.form.getInputProps(column.name)}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            readOnly={effectiveReadOnly}
            data-autofocus={resolvedAutoFocus ? true : undefined}
            autoFocus={resolvedAutoFocus}
            precision={precision ?? column.scale}
            maw={resolved_maw}
            miw={resolved_miw}


            // FIX for mantine NumberInput not supporting null
            value={entityForm.form.values[column.name] === null ? '' : entityForm.form.values[column.name] as number}

            ref={ref}
        />
    )

});