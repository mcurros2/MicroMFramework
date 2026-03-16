import { useProps } from "@mantine/core";
import { useEffect, useMemo } from "react";
import { NumberField, NumberFieldProps } from "./NumberField";
import { moneyFormatter } from "./numberInputFormatters";

export interface MoneyRangeFieldProps extends NumberFieldProps {
    currencySymbol?: string,
    millionsStep?: number,
    thousandsStep?: number,
}

export const MoneyRangeFieldDefaultProps: Partial<MoneyRangeFieldProps> = {
    currencySymbol: '$',
    millionsStep: 50000,
    thousandsStep: 1000,
    step: 100
}

export function MoneyRangeField(props: MoneyRangeFieldProps) {
    const {
        currencySymbol, entityForm, column, millionsStep, thousandsStep, step, ...rest
    } = useProps('MoneyRangeField', MoneyRangeFieldDefaultProps, props);

    useEffect(() => {
        const rawValue = entityForm.form.values[column.name];
        const formattedValue = moneyFormatter((rawValue ?? '').toString(), '');
        column.valueDescription = `${currencySymbol} ${formattedValue}`;
    }, [column, currencySymbol, entityForm.form.values]);

    const variable_step = useMemo(
        () => {
            const value = entityForm.form.values[column.name] as number;
            return value >= 1000000 ? millionsStep : value >= 1000 ? thousandsStep : step || 1 as number
        },
        [column.name, entityForm.form.values, millionsStep, step, thousandsStep]
    )

    return (
        <NumberField
            {...rest}
            column={column}
            entityForm={entityForm}
            step={variable_step}
            stepHoldDelay={500}
            stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
            prefix={currencySymbol}
        />
    )
}
