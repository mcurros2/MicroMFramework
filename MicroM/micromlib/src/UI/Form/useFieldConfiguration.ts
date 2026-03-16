import { useProps } from "@mantine/core";
import { ReactNode, useEffect } from "react";
import { SQLType, Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { combineValidators, CommonValidators, CommonValidatorsType, ValidationRule, ValidatorConfiguration } from "../../Validation";
import { UseEntityFormReturnType } from "./useEntityForm";

export type UseFieldConfigurationProps = {
    entityForm: UseEntityFormReturnType, // Adjust type according to your definition
    column: EntityColumn<Value>,
    required?: boolean,
    requiredMessage?: ReactNode,
    validate?: ValidatorConfiguration,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    readOnly?: boolean
};

export const UseFieldConfigurationDefaultProps: Partial<UseFieldConfigurationProps> = {
    requiredMessage: "A value is required"
}

export const useFieldConfiguration = (props: UseFieldConfigurationProps) => {
    const { validationContainer, validate, entityForm, column, required, requiredMessage, readOnly } = useProps('UseFieldConfigurationProps', UseFieldConfigurationDefaultProps, props);

    useEffect(() => {
        const config: ValidatorConfiguration = validate ?? {};

        // MMC: Add required if not added in validators
        if (!config.required && !readOnly && !(entityForm.formMode === 'view')
            && (required ?? !column.hasFlag(EntityColumnFlags.nullable))
        ) config['required'] = { message: requiredMessage };

        const validators: ValidationRule[] = Object.entries(config).map(([key, config]) => {
            const validatorFunction = CommonValidators[key as keyof CommonValidatorsType];
            if (validatorFunction) {
                const message = config.message ?? '';
                const validator = (config.data) ? validatorFunction(config.data, message) : validatorFunction(message);
                return (value, values) => {
                    const result = validator(value, values);
                    return result === false ? null : result ?? null;
                };
            }
            return () => null;
        });

        if (validators.length > 0) {
            const combinedValidator = combineValidators(validationContainer, ...validators);
            entityForm.configureField(column, combinedValidator);
        } else {
            entityForm.configureField(column);
        }

        // MMC: ensure that the field is present in form.values
        // This is important when conditionally rendering fields
        if (!Object.keys(entityForm.form.values).includes(column.name)) {
            const date_types: SQLType[] = ['date', 'datetime', 'datetime2', 'smalldatetime'];
            if (date_types.includes(column.type)) {
                entityForm.form.setFieldValue(column.name, column.value);
            }
            else {
                entityForm.form.setFieldValue(column.name, column.value ?? '');
            }
        }

    }, [validate, entityForm, column, validationContainer, required, requiredMessage, readOnly]);

};
