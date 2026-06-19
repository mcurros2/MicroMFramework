import { useComponentDefaultProps } from "@mantine/core";
import { useCallback, useMemo } from "react";
import { NumberField, NumberFieldProps } from "./NumberField";
import { bytesFormatter, bytesParser } from "./numberInputFormatters";

export interface BytesRangeFieldProps extends Omit<NumberFieldProps, 'parser' | 'formatter'> {
    gigaBytesStep?: number,
    megaBytesStep?: number,
    kiloBytesStep?: number,
}

export const BytesRangeFieldDefaultProps: Partial<BytesRangeFieldProps> = {
    gigaBytesStep: 0.5,
    megaBytesStep: 5,
    kiloBytesStep: 50,
    step: 1000
}

export function BytesRangeField(props: BytesRangeFieldProps) {
    const {
        entityForm, column, gigaBytesStep, megaBytesStep, kiloBytesStep, step, ...rest
    } = useComponentDefaultProps('BytesRangeField', BytesRangeFieldDefaultProps, props);


    const formatter = useCallback((value: string) => {
        const formattedValue = bytesFormatter(value);
        column.valueDescription = formattedValue;
        return formattedValue;
    }, [column]);

    const variable_step = useMemo(
        () => {
            const value = entityForm.form.values[column.name] as number;
            return value >= 1073741824 ? gigaBytesStep : value >= 1048576 ? megaBytesStep : value >= 1024 ? kiloBytesStep : step || 1 as number
        },
        [entityForm.form.values, column.name, gigaBytesStep, megaBytesStep, kiloBytesStep, step]
    );

    return (
        <NumberField
            {...rest}
            column={column}
            entityForm={entityForm}
            parser={bytesParser}
            formatter={formatter}
            step={variable_step}
            stepHoldDelay={500}
            stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
        />
    )
}