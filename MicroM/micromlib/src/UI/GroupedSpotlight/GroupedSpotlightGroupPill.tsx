import { Button, ButtonProps, Text, TextProps, useComponentDefaultProps } from "@mantine/core";
import { MouseEvent, ReactNode } from "react";

export interface GroupedSpotlightGroupPillProps extends Omit<ButtonProps, 'children' | 'variant'> {
    label: ReactNode,
    count: number,
    active?: boolean,
    onClick?: () => void,
    activeVariant?: ButtonProps['variant'],
    inactiveVariant?: ButtonProps['variant'],
    countProps?: Omit<TextProps, 'children'>
}

export const GroupedSpotlightGroupPillDefaultProps: Partial<GroupedSpotlightGroupPillProps> = {
    size: 'xs',
    radius: 'xl',
    activeVariant: 'light',
    inactiveVariant: 'default',
    countProps: { span: true, ml: 6, fw: 400, opacity: 0.7 }
};

// Keeps the focus in the search input, so the arrow keys keep working after a click on a pill
const keepFocus = (event: MouseEvent) => event.preventDefault();

export function GroupedSpotlightGroupPill(props: GroupedSpotlightGroupPillProps) {
    const {
        label, count, active, onClick, activeVariant, inactiveVariant, countProps, ...others
    } = useComponentDefaultProps('GroupedSpotlightGroupPill', GroupedSpotlightGroupPillDefaultProps, props);

    return (
        <Button
            {...others}
            variant={active ? activeVariant : inactiveVariant}
            aria-pressed={!!active}
            tabIndex={-1}
            onMouseDown={keepFocus}
            onClick={onClick}
        >
            {label}
            <Text {...countProps}>{count}</Text>
        </Button>
    );
}
