import { Group, PasswordInput, PasswordInputProps, useComponentDefaultProps } from "@mantine/core";
import { ReactNode } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfigurationParms } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { FieldAutoFocus, resolveFieldAutoFocus, resolveFieldReadOnly, resolveFieldWidths } from "./fieldUIHelpers";
import { MicroMWidthSizes } from "./types";


type PasswordFieldAllowedValidators = 'regex' | 'length' | 'field' | 'required';
export type PasswordFieldValidatorConfiguration = Partial<Record<PasswordFieldAllowedValidators, ValidatorConfigurationParms>>;

export interface PasswordFieldProps extends Omit<PasswordInputProps, 'autoFocus'> {
    column: EntityColumn<string>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    validate?: PasswordFieldValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>,
    autoFocus?: FieldAutoFocus,
    autoMaxWidth?: { columnLenghtLessThanOrEqual: number, maxWidth: string },
    maxWidth?: keyof typeof MicroMWidthSizes,
    minWidth?: keyof typeof MicroMWidthSizes,
}

const defaultProps: Partial<PasswordFieldProps> = {
    validationContainer: Group,
    autoMaxWidth: { columnLenghtLessThanOrEqual: 20, maxWidth: '13rem' },
}

export function PasswordField(props: PasswordFieldProps) {

    const {
        column, loading, entityForm, maw, miw, required, maxLength, disabled, disableOnLoading, label, validationContainer, validate, requiredMessage,
        description, readOnly, withAsterisk, autoMaxWidth, maxWidth, minWidth, autoFocus, ...others
    } = useComponentDefaultProps('PasswordField', defaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;


    const resolvedAutoFocus = resolveFieldAutoFocus(autoFocus, entityForm.formMode, entityForm.status.loading);
    const effectiveReadOnly = resolveFieldReadOnly(column, entityForm.formMode, readOnly);
    const { maw: resolved_maw, miw: resolved_miw } = resolveFieldWidths({ maw, miw, maxWidth, minWidth });

    return (
        <PasswordInput
            {...others}
            {...entityForm.form.getInputProps(column.name)}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            description={showDescription ? (description ?? column.description) : ''}
            maw={resolved_maw ?? ((column.length <= autoMaxWidth!.columnLenghtLessThanOrEqual) ? autoMaxWidth!.maxWidth : undefined)}
            miw={resolved_miw}
            maxLength={maxLength ?? column.length}
            readOnly={effectiveReadOnly}
            autoFocus={resolvedAutoFocus}
            data-autofocus={resolvedAutoFocus ? true : undefined}
            disabled={(disableOnLoading) ? loading : disabled}

        />
    );
}