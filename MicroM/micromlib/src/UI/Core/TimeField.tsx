import { ActionIcon, useComponentDefaultProps } from "@mantine/core";
import { TimeInput, TimeInputProps } from "@mantine/dates";
import { IconClock } from "@tabler/icons-react";
import { ReactNode, forwardRef, useImperativeHandle, useRef } from "react";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { ValidatorConfiguration } from "../../Validation";
import { Value } from "../../client";
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form";

export interface TimeFieldProps extends Omit<TimeInputProps, 'validate'> {
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    validate?: ValidatorConfiguration,
    requiredMessage?: React.ReactNode,
    validationContainer?: React.ComponentType<{ children: ReactNode }>
    autoFocus?: boolean,
    showTimePicker?: boolean,
}

export const TimeFieldDefaultProps: Partial<TimeFieldProps> = {
    showTimePicker: true,
    maw: '20rem',
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
        label, placeholder, description, withAsterisk, autoFocus,
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

    return (
        <TimeInput
            {...others}
            withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            label={label ?? column.prompt}
            placeholder={placeholder ?? column.placeholder}
            description={showDescription ? (description ?? column.description) : ''}
            data-autofocus={autoFocus}
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
