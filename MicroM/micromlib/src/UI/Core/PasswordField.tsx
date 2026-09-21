import { Group, PasswordInput, PasswordInputProps, useComponentDefaultProps } from "@mantine/core";
import { ReactNode } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfigurationParms } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { MicroMWidthSizes } from "./types";


type PasswordFieldAllowedValidators = 'regex' | 'length' | 'field' | 'required';
export type PasswordFieldValidatorConfiguration = Partial<Record<PasswordFieldAllowedValidators, ValidatorConfigurationParms>>;

export interface PasswordFieldProps extends PasswordInputProps {
    column: EntityColumn<string>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    validate?: PasswordFieldValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>,
    autoMaxWidth?: { columnLenghtLessThanOrEqual: number, maxWidth: string },
    maxWidth?: keyof typeof MicroMWidthSizes,
    minWidth?: keyof typeof MicroMWidthSizes,
}

const defaultProps: Partial<PasswordFieldProps> = {
    validationContainer: Group,
    autoMaxWidth: { columnLenghtLessThanOrEqual: 20, maxWidth: '20rem' },
}

export function PasswordField(props: PasswordFieldProps) {

    const {
        column, loading, entityForm, maw, miw, required, maxLength, disabled, disableOnLoading, label, validationContainer, validate, requiredMessage,
        description, readOnly, withAsterisk, autoMaxWidth, maxWidth, minWidth, ...others
    } = useComponentDefaultProps('PasswordField', defaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    const resolved_maw = maw ?? (maxWidth !== 'auto' && maxWidth !== undefined) ? MicroMWidthSizes[maxWidth!] : undefined;
    const resolved_miw = miw ?? (minWidth !== 'auto' && minWidth !== undefined) ? MicroMWidthSizes[minWidth!] : undefined;

    return (
        <PasswordInput
            {...others}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            description={showDescription ? (description ?? column.description) : ''}
            maw={resolved_maw ?? ((column.length <= autoMaxWidth!.columnLenghtLessThanOrEqual) ? autoMaxWidth!.maxWidth : undefined)}
            miw={resolved_miw}
            maxLength={maxLength ?? column.length}
            readOnly={entityForm.formMode === 'view' ? true : readOnly}
            disabled={(disableOnLoading) ? loading : disabled}
            {...entityForm.form.getInputProps(column.name)}
        />
    );
}