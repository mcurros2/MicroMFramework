import { ActionIcon, useComponentDefaultProps } from "@mantine/core";
import { TimeInput, TimeInputProps } from "@mantine/dates";
import { IconClock } from "@tabler/icons-react";
import { forwardRef, ReactNode, useImperativeHandle, useRef } from "react";
import { Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";
import { MicroMWidthSizes } from "./types";

export interface TimeFieldProps extends Omit<TimeInputProps, 'validate' | 'autoFocus'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: ValidatorConfiguration,
    requiredMessage?: React.ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean,
    showTimePicker?: boolean,
    maxWidth?: keyof typeof MicroMWidthSizes,
    minWidth?: keyof typeof MicroMWidthSizes,
}

export const TimeFieldDefaultProps: Partial<TimeFieldProps> = {
    showTimePicker: true,
    maxWidth: "sm"
}

// Define the methods and properties you want to expose
interface TimeFieldRef {
    showPicker: () => void;
}

// Extend the HTMLInputElement to include the showPicker method
interface ExtendedHTMLInputElement extends HTMLInputElement {
    showPicker: () => void;
}

export const TimeField = forwardRef<TimeFieldRef, TimeFieldProps>(function TimeField(props: TimeFieldProps, ref) {
    const {
        entityForm, column, validationContainer, validate, required, requiredMessage, readOnly, showTimePicker,
        label, placeholder, description, withAsterisk, autoFocus, maw, miw, maxWidth, minWidth,
        ...others
    } = useComponentDefaultProps('TimeField', TimeFieldDefaultProps, props);

    useFieldConfiguration({ entityForm, column, validationContainer, validate, required, requiredMessage, readOnly });

    const [showDescription,] = entityForm.showDescriptionState;
    const clockRef = useRef<ExtendedHTMLInputElement | null>(null);

    // Expose the showPicker method to the parent using the ref
    useImperativeHandle(ref, () => ({
        showPicker: () => {
            clockRef.current?.showPicker?.();
        }
    }), []);

    const { formMode, status } = entityForm;
    const add_autofocus = formMode === 'add' ? true : undefined;
    const edit_autofocus = status.loading === false && formMode !== 'add' ? true : undefined;

    const resolved_maw = maw ?? (maxWidth !== undefined) ? MicroMWidthSizes[maxWidth!] : undefined
    const resolved_miw = miw ?? (minWidth !== undefined) ? MicroMWidthSizes[minWidth!] : undefined;

    return (
        <TimeInput
            {...others}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            maw={resolved_maw}
            miw={resolved_miw}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            data-autofocus={autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus}
            autoFocus={autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus}
            ref={clockRef}
            rightSection={showTimePicker &&
                <ActionIcon disabled={readOnly} onClick={() => clockRef.current?.showPicker?.()}>
                    <IconClock size="1rem" stroke={1.5} />
                </ActionIcon>
            }
            rightSectionWidth={showTimePicker ? "2.5rem" : undefined}

            {...entityForm.form.getInputProps(column.name)}
        />
    );
});
