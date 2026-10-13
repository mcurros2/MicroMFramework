import { Switch, SwitchProps } from "@mantine/core";
import { ReactNode } from "react";
import { Value } from "../../client";
import { EntityColumn } from "../../Entity";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { FieldAutoFocus, FieldWidthProps, resolveFieldAutoFocus, resolveFieldReadOnly, resolveFieldWidths } from "./fieldUIHelpers";

export interface SwitchFieldProps extends Omit<SwitchProps, 'autoFocus'>, FieldWidthProps {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    autoFocus?: FieldAutoFocus,
    requiredMessage?: ReactNode
}
export function SwitchField(props: SwitchFieldProps) {

    const { column, required, entityForm, loading, disableOnLoading, label, readOnly, disabled, requiredMessage, description, autoFocus, maw, miw, maxWidth, minWidth, ...others } = props;

    useFieldConfiguration({ entityForm, column, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    const effectiveReadOnly = resolveFieldReadOnly(column, entityForm.formMode, readOnly);
    const resolvedAutoFocus = resolveFieldAutoFocus(autoFocus, entityForm.formMode, entityForm.status.loading);
    const widths = resolveFieldWidths({ maw, miw, maxWidth, minWidth });
    const inputProps = entityForm.form.getInputProps(column.name, { type: 'checkbox' });

    return (
        <Switch
            {...others}
            label={label ?? column.prompt}
            description={showDescription ? (description ?? column.description) : ''}
            {...inputProps}
            {...widths}
            disabled={effectiveReadOnly || (disabled ?? (disableOnLoading && loading))}
            autoFocus={resolvedAutoFocus}
            data-autofocus={resolvedAutoFocus ? true : undefined}
        />);

}