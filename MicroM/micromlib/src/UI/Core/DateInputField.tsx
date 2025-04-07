import dayjs from "dayjs";
import customParseFormat from 'dayjs/plugin/customParseFormat';

import { Group, useComponentDefaultProps } from "@mantine/core";
import { DateInput, DateInputProps } from "@mantine/dates";
import { IconCalendar } from "@tabler/icons-react";
import { ReactNode, forwardRef } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { Value } from "../../client";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";

dayjs.extend(customParseFormat);

export interface DateInputFieldProps extends DateInputProps {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: boolean
}

export const DateInputFieldDefaultProps: Partial<DateInputFieldProps> = {
    validationContainer: Group,
    maw: '20rem',
    icon: <IconCalendar size="1rem" />,
    allowDeselect: true,
    valueFormat: 'YYYY-MM-DD',
    popoverProps: {
        withinPortal: true,
        zIndex: 10000
    }
}
export const DateInputField = forwardRef<HTMLInputElement, DateInputFieldProps>(function DateInputField(props, ref) {
    const {
        column, entityForm, validate, validationContainer, required, requiredMessage, readOnly, label,
        placeholder, description, withAsterisk, autoFocus, clearable, valueFormat, ...others
    } = useComponentDefaultProps('DateInputField', DateInputFieldDefaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    return (
        <DateInput
            {...others}
            valueFormat={valueFormat}
            required={required ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            clearable={clearable ?? (!readOnly && !(entityForm.formMode === 'view'))}
            data-autofocus={autoFocus}
            readOnly={readOnly || entityForm.formMode === 'view'}
            {...entityForm.form.getInputProps(column.name)}
            ref={ref}
        />
    )
});