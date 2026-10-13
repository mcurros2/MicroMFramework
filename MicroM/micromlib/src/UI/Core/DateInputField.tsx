import { Group, useComponentDefaultProps } from "@mantine/core";
import { DateInput, DateInputProps } from "@mantine/dates";
import { IconCalendar } from "@tabler/icons-react";
import dayjs from "dayjs";
import customParseFormat from 'dayjs/plugin/customParseFormat';
import { forwardRef, ReactNode } from "react";
import { Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { resolveFieldAutoFocus, resolveFieldReadOnly, resolveFieldWidths } from "./fieldUIHelpers";
import { MicroMWidthSizes } from "./types";

dayjs.extend(customParseFormat);

export interface DateInputFieldProps extends Omit<DateInputProps, 'autoFocus'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: ValidatorConfiguration,
    requiredMessage?: ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean,
    maxWidth?: keyof typeof MicroMWidthSizes,
    minWidth?: keyof typeof MicroMWidthSizes,
}

export const DateInputFieldDefaultProps: Partial<DateInputFieldProps> = {
    validationContainer: Group,
    maxWidth: 'sm',
    minWidth: 'sm',
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
        placeholder, description, withAsterisk, autoFocus, clearable, valueFormat, maxWidth, minWidth,
        maw, miw, ...others
    } = useComponentDefaultProps('DateInputField', DateInputFieldDefaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;

    const resolvedAutoFocus = resolveFieldAutoFocus(autoFocus, entityForm.formMode, entityForm.status.loading);
    const effectiveReadOnly = resolveFieldReadOnly(column, entityForm.formMode, readOnly);
    const { maw: resolved_maw, miw: resolved_miw } = resolveFieldWidths({ maw, miw, maxWidth, minWidth });

    return (
        <DateInput
            {...others}
            {...entityForm.form.getInputProps(column.name)}
            valueFormat={valueFormat}
            required={required ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            clearable={!effectiveReadOnly && (clearable ?? true)}
            data-autofocus={resolvedAutoFocus ? true : undefined}
            autoFocus={resolvedAutoFocus}
            readOnly={effectiveReadOnly}
            maw={resolved_maw}
            miw={resolved_miw}

            ref={ref}
        />
    )
});