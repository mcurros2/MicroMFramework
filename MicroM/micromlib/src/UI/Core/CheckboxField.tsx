import { Checkbox, CheckboxProps } from "@mantine/core";
import { ReactNode } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { Value } from "../../client";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";

export interface CheckboxFieldProps extends CheckboxProps {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    requiredMessage?: ReactNode
}
export function CheckboxField(props: CheckboxFieldProps) {

    const { column, required, entityForm, loading, disableOnLoading, label, readOnly, disabled, requiredMessage, description, ...others } = props;

    useFieldConfiguration({ entityForm, column, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    return (
        <Checkbox
            {...others}
            label={label ?? column.prompt}
            description={showDescription ? (description ?? column.description) : ''}
            disabled={disabled ?? (entityForm.formMode === 'view' || (disableOnLoading && loading) || readOnly)}
            required={(!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            {...entityForm.form.getInputProps(column.name, { type: 'checkbox' })}
        />);

}