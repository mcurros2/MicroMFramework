import { Card, CardProps, Center, DefaultMantineColor, Group, MantineColor, RingProgress, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { IconProps } from "@tabler/icons-react";
import { ReactNode } from "react";
import { EntityColumn } from "../../Entity";

export interface RingProgressFieldProps extends Omit<CardProps, 'children'> {
    column: EntityColumn<number>,
    loading?: boolean,
    title?: string,
    description?: string,
    centerIcon?: (props: IconProps) => ReactNode,
    centerLabel?: ReactNode,
    maxValue?: number,
    color?: DefaultMantineColor,
    ringSize?: number,
    thickness?: number,
    iconSize?: string | number,
    displayPercent?: 'percent' | 'fraction' | 'none',
    showPercentAsCenterLabel?: boolean,
    iconColor?: MantineColor,
}

export const RingProgressFieldDefaultProps: Partial<RingProgressFieldProps> = {
    maxValue: 100,
    ringSize: 65,
    thickness: 7,
    iconSize: "1.1rem",
    displayPercent: 'none',
    color: 'blue',
    p: "xs",
    withBorder: true
}

export function RingProgressField(props: RingProgressFieldProps) {
    const {
        centerIcon, column, title, description, color, maxValue, ringSize, thickness, iconSize, displayPercent,
        centerLabel, showPercentAsCenterLabel, iconColor, ...others
    } = useComponentDefaultProps('RingProgressField', RingProgressFieldDefaultProps, props);

    const theme = useMantineTheme();

    const Icon = centerIcon!;

    const progress = isNaN(((column.value ?? 0) / maxValue!) * 100) ? 0 : ((column.value ?? 0) / maxValue!) * 100;

    const percentIconLabel = showPercentAsCenterLabel ? <Text weight="bolder" align="center" color={iconColor} size="xs">{progress.toFixed(0)}%</Text> : undefined;

    const iconLabel = centerIcon ? <Center><Icon size={iconSize} stroke={1.5} color={iconColor} /></Center> : undefined

    const percent = (displayPercent === 'none' || undefined) ? '' : (displayPercent === 'percent' ? ` ${progress.toFixed(2)}` : ` ${column.value}/${maxValue}`);

    return (
        <Card key={column.name} {...others}>
            <Group>
                <RingProgress
                    size={ringSize}
                    roundCaps
                    thickness={thickness}
                    sections={[{ value: progress, color: color! }]}
                    label={iconLabel || percentIconLabel || centerLabel}
                />
                <div style={{ flex: 1 }}>
                    <Text weight={500} color={theme.colorScheme === 'dark' ? theme.white : theme.colors.gray[9]} size="sm">
                        {title ?? column.prompt}{percent}
                    </Text>
                    <Text size="sm" color={theme.colorScheme === 'dark' ? theme.colors.dark[2] : theme.colors.gray[6]}>
                        {description ?? column.description}
                    </Text>
                </div>
            </Group>
        </Card>
    )
}