import { useComponentDefaultProps } from "@mantine/core";
import { useCallback, useMemo } from "react";
import { NumberField, NumberFieldProps } from "./NumberField";
import { moneyFormatter, moneyParser } from "./numberInputFormatters";

export interface MoneyRangeFieldProps extends Omit<NumberFieldProps, 'parser' | 'formatter'> {
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
    } = useComponentDefaultProps('MoneyRangeField', MoneyRangeFieldDefaultProps, props);

    const formatter = useCallback((value: string) => {
        const formattedValue = moneyFormatter(value, currencySymbol!);
        column.valueDescription = formattedValue;
        return formattedValue;
    }, [currencySymbol]);

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
            parser={moneyParser}
            formatter={formatter}
            step={variable_step}
            stepHoldDelay={500}
            stepHoldInterval={(t) => Math.max(1000 / t ** 2, 25)}
        />
    )
}