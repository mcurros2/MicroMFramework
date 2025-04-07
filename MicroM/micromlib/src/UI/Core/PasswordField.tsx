import { Group, PasswordInput, PasswordInputProps, useComponentDefaultProps } from "@mantine/core";
import { ReactNode } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfigurationParms } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";


type PasswordFieldAllowedValidators = 'regex' | 'length' | 'field' | 'required';
export type PasswordFieldValidatorConfiguration = Partial<Record<PasswordFieldAllowedValidators, ValidatorConfigurationParms>>;

export interface PasswordFieldProps extends PasswordInputProps {
    column: EntityColumn<string>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    validate?: PasswordFieldValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
}

const defaultProps: Partial<PasswordFieldProps> = {
    validationContainer: Group
}

export function PasswordField(props: PasswordFieldProps) {

    const {
        column, loading, entityForm, maw, required, maxLength, disabled, disableOnLoading, label, validationContainer, validate, requiredMessage,
        description, readOnly, withAsterisk, ...others
    } = useComponentDefaultProps('PasswordField', defaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    return (
        <PasswordInput
            {...others}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            description={showDescription ? (description ?? column.description) : ''}
            maw={maw ?? ((column.length <= 20) ? '10rem' : undefined)}
            maxLength={maxLength ?? column.length}
            readOnly={entityForm.formMode === 'view' ? true : readOnly}
            disabled={(disableOnLoading) ? loading : disabled}
            {...entityForm.form.getInputProps(column.name)}
        />
    );
}