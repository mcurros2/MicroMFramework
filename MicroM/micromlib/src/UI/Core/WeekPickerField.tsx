import { Accordion, AccordionProps, Group, MantineSize, Stack, Text, getSize, rem, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconCalendar } from "@tabler/icons-react";
import { ReactNode, useState } from "react";
import { WeekPicker, WeekPickerProps, useLocaleFormat } from ".";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";

export interface WeekPickerFieldProps extends Omit<WeekPickerProps, 'weekStartDatevalue' | 'setWeekStartValue' | 'setWeekEndValue'> {
    weekStartDateColumn: EntityColumn<Date | null>,
    weekEndDateColumn?: EntityColumn<Date | null>,
    entityForm: UseEntityFormReturnType,
    loading?: boolean,
    disableOnLoading?: boolean,
    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>,
    accordionVariant?: AccordionProps['variant'],
    accordionExpanded?: boolean,

    placeholder?: string,
    size?: MantineSize,
    required?: boolean,
    readonly?: boolean,
    disabled?: boolean,
    label?: string,
    description?: string,

}

export const WeekPickerFieldDefaultProps: Partial<WeekPickerFieldProps> = {
    placeholder: 'Select a week',
    accordionVariant: 'separated',
    allowDeselect: true,
    size: 'sm',
    requiredMessage: 'A value is required',
}

export function WeekPickerField(props: WeekPickerFieldProps) {
    const {
        weekStartDateColumn, weekEndDateColumn, entityForm, validate, validationContainer, requiredMessage,
        accordionVariant, accordionExpanded, placeholder, size, required, readonly, disabled, label, description,
        ...others
    } = useComponentDefaultProps('WeekPickerField', WeekPickerFieldDefaultProps, props);

    const theme = useMantineTheme();
    const localeFormat = useLocaleFormat({});

    useFieldConfiguration({ entityForm, column: weekStartDateColumn, validationContainer, validate, required: false, requiredMessage, readOnly: false });

    const controlSize = getSize({ size: size ?? "sm", sizes: theme.fontSizes });
    const descriptionSize = getSize({ size: size ?? "sm", sizes: theme.fontSizes });
    const labelColor = theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[9];
    const descriptionColor = theme.colorScheme === 'dark' ? theme.colors.dark[2] : theme.colors.gray[6];


    const [weekEndValue, setWeekEndValue] = useState<Date | null>(weekEndDateColumn?.value || weekEndDateColumn?.defaultValue || null);

    const weekStartDisplayValue = localeFormat.formatValue(weekStartDateColumn.value, 'date');
    const weekEndDisplayValue = weekEndValue ? localeFormat.formatValue(weekEndValue, 'date') : '';

    const is_required = (required ?? (!readonly && !(entityForm.formMode === 'view') && !weekStartDateColumn.hasFlag(EntityColumnFlags.nullable)));
    const error_color = theme.fn.variant({ variant: 'filled', color: 'red' }).background;

    return (
        <Stack style={{ gap: "0.1rem" }}>
            <Group style={{ gap: "0.2rem" }}>
                <Text size={controlSize} weight="500" color={labelColor}>{label ?? weekStartDateColumn.prompt}</Text>
                {is_required && <Text size={controlSize} weight="500" color={theme.colors.red[5]}>*</Text>}
            </Group>
            {(description ?? weekStartDateColumn.description) &&
                <Text style={{ fontSize: `calc(${descriptionSize} - ${rem(2)})`, lineHeight: 1.2 }} color={descriptionColor}>{description ?? weekStartDateColumn.description}</Text>
            }
            <Accordion
                style={{
                    marginTop: `calc(${theme.spacing.xs} / 2)`,
                    border: `${rem(1)} solid ${(is_required && !weekStartDateColumn.value) ? error_color : theme.colorScheme === 'dark' ? theme.colors.dark[4] : theme.colors.gray[4]}`,
                    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.white,
                    transition: 'border-color 100ms ease',
                    borderRadius: theme.radius.sm,
                }}
                variant={accordionVariant}
                defaultValue={accordionExpanded ? `weekPicker-${weekStartDateColumn.name}` : undefined}
                styles={{
                    label: { paddingLeft: "0", paddingTop: "0.447rem", paddingBottom: "0.447rem", paddingRight: "0.625rem" },
                    control: { paddingLeft: "0.75rem", paddingRight: "0.625rem" }
                }}
                maw="20rem"
            >
                <Accordion.Item value={`weekPicker-${weekStartDateColumn.name}`}>
                    <Accordion.Control icon={<IconCalendar size="1rem" />}>
                        {(weekStartDisplayValue && weekStartDisplayValue !== 'null') ? (
                            <Text size={controlSize}>
                                {weekStartDisplayValue}{weekEndDisplayValue ? ` - ${weekEndDisplayValue}` : ''}
                            </Text>
                        ) : (
                            <Text color="dimmed" size={controlSize}>{placeholder}</Text>
                        )}
                    </Accordion.Control>
                    <Accordion.Panel>
                        <WeekPicker
                            {...others}
                            static={ readonly || disabled }
                            weekStartDatevalue={entityForm.form.values[weekStartDateColumn.name] as Date | null}
                            setWeekStartValue={(value: Date | null) => {
                                entityForm.form.setFieldValue(weekStartDateColumn.name, value);
                                weekStartDateColumn.value = value;
                            }}
                            setWeekEndValue={(value: Date | null) => {
                                if (weekEndDateColumn) {
                                    entityForm.form.setFieldValue(weekEndDateColumn.name, value);
                                    weekEndDateColumn.value = value;
                                }
                                setWeekEndValue(value);
                            }}
                        />
                    </Accordion.Panel>
                </Accordion.Item>
            </Accordion>
            {
                (is_required && !weekStartDateColumn.value) && <Text size="xs" color={error_color}>{requiredMessage}</Text>
            }
        </Stack>
    );
}