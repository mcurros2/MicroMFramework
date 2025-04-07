import { Group, MantineSize, PinInput, PinInputProps, Stack, Text, getSize, rem, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { ReactNode } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { Value } from "../../client";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { useTextTransform, useTextTransformProps } from "./useTextTransform";

export interface PinFieldProps extends Omit<PinInputProps, 'validate'>, Omit<useTextTransformProps, 'entityForm' | 'column'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: ValidatorConfiguration,
    requiredMessage?: React.ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: boolean,
    size?: MantineSize,
    description?: string,
    label?: string,
}


export const PinFieldDefaultProps: Partial<PinFieldProps> = {
    transform: 'uppercase',
    requiredMessage: "A value is required",
    size: "sm"
}

function isArrayOfStrings(value: ReactNode): value is string[] {
    return Array.isArray(value) && value.every(item => typeof item === 'string');
}

export function PinField(props: PinFieldProps) {
    const {
        entityForm, column, validationContainer, validate, required, requiredMessage, readOnly,
        placeholder, autoFocus, transform, autoTrim, size, description, label,
        ...others
    } = useComponentDefaultProps('PinField', PinFieldDefaultProps, props);

    const theme = useMantineTheme();

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const textTransform = useTextTransform({ entityForm, column, transform, autoTrim });

    const handleOnComplete = (value: string) => {
        textTransform(value);
    }

    const controlSize = getSize({ size: size ?? "sm", sizes: theme.fontSizes });
    const descriptionSize = getSize({ size: size ?? "sm", sizes: theme.fontSizes });
    const labelColor = theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[9];
    const descriptionColor = theme.colorScheme === 'dark' ? theme.colors.dark[2] : theme.colors.gray[6];

    const { form } = entityForm;

    return (
        <Stack style={{ gap: "0.1rem" }}>
            <Group style={{ gap: "0.2rem" }}>
                <Text size={controlSize} weight="500" color={labelColor}>{label ?? column.prompt}</Text>
                {(required ?? (!readOnly && !(entityForm.formMode === 'view') && !column.hasFlag(EntityColumnFlags.nullable))) && <Text size={controlSize} weight="500" color={theme.colors.red[5]}>*</Text>}
            </Group>
            {(description ?? column.description) &&
                <Text style={{ fontSize: `calc(${descriptionSize} - ${rem(2)})`, lineHeight: 1.2 }} color={descriptionColor}>{description ?? column.description}</Text>
            }
            <PinInput
                {...others}
                {...entityForm.form.getInputProps(column.name)}
                placeholder={placeholder ?? column.placeholder}
                autoFocus={autoFocus}
                onComplete={handleOnComplete}
                required={required ?? !column.hasFlag(EntityColumnFlags.nullable)}
                mt="xs"
            />
            {form.errors[column.name] &&
                <Group>
                    <Text size="xs" color="red">
                        {(() => {
                            const errorValue = form.errors[column.name];
                            if (isArrayOfStrings(errorValue)) {
                                return errorValue.join(', ');
                            } else if (errorValue !== null && errorValue !== undefined) {
                                return errorValue;
                            } else {
                                return '';
                            }
                        })()}
                    </Text>
                </Group>
            }
        </Stack>
    )
}
