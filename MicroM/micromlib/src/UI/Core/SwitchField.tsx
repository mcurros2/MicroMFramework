import { Switch, SwitchProps } from "@mantine/core";
import { ReactNode } from "react";
import { Value } from "../../client";
import { EntityColumn } from "../../Entity";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";

export interface SwitchFieldProps extends SwitchProps {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    requiredMessage?: ReactNode
}
export function SwitchField(props: SwitchFieldProps) {

    const { column, required, entityForm, loading, disableOnLoading, label, readOnly, disabled, requiredMessage, description, ...others } = props;

    useFieldConfiguration({ entityForm, column, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    return (
        <Switch
            {...others}
            label={label ?? column.prompt}
            description={showDescription ? (description ?? column.description) : ''}
            disabled={disabled ?? (entityForm.formMode === 'view' || (disableOnLoading && loading) || readOnly)}
            {...entityForm.form.getInputProps(column.name, { type: 'checkbox' })}
        />);

}