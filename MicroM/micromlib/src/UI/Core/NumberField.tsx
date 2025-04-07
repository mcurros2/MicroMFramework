import { Group, NumberInput, NumberInputProps, useComponentDefaultProps } from "@mantine/core";
import { ReactNode, forwardRef } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { Value } from "../../client";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";


export interface NumberFieldProps extends NumberInputProps {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: boolean
}

const defaultProps: Partial<NumberFieldProps> = {
    validationContainer: Group
}

// MMC: This component in mantine has a bug: if you set setFieldValue and the field has autofocus, the value displayed is not set until onBlur.

export const NumberField = forwardRef<HTMLInputElement, NumberFieldProps>(function NumberField(props: NumberFieldProps, ref) {

    const {
        column, entityForm, validate, validationContainer, required, requiredMessage, readOnly, label,
        placeholder, description, withAsterisk, autoFocus, ...others
    } = useComponentDefaultProps('NumberField', defaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    // MMC: disabled when loading is done at the form level in EntityForm and fieldset
    //data-autofocus={autoFocus}  disabled={(disableOnLoading) ? loading : disabled}
    return (
        <NumberInput
            {...others}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            readOnly={entityForm.formMode === 'view' ? true : readOnly}
            data-autofocus={autoFocus}
            precision={column.scale}
            {...entityForm.form.getInputProps(column.name)}
            ref={ref}
        />
    )

});