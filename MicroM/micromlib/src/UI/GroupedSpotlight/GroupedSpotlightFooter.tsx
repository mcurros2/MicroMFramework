import { Group, GroupProps, Kbd, KbdProps, Text, TextProps, useComponentDefaultProps } from "@mantine/core";
import { ReactNode } from "react";

export interface GroupedSpotlightFooterHint {
    keys: string[],
    label: ReactNode
}

export interface GroupedSpotlightFooterProps extends Omit<GroupProps, 'children'> {
    hints?: GroupedSpotlightFooterHint[],
    hintProps?: Omit<GroupProps, 'children'>,
    kbdProps?: Omit<KbdProps, 'children'>,
    textProps?: Omit<TextProps, 'children'>
}

export const GroupedSpotlightFooterDefaultProps: Partial<GroupedSpotlightFooterProps> = {
    hints: [
        { keys: ['↑', '↓'], label: 'navigate' },
        { keys: ['↵'], label: 'select' },
        { keys: ['esc'], label: 'close' }
    ],
    spacing: 'lg',
    px: 'md',
    py: 10,
    hintProps: { spacing: 6 },
    kbdProps: { size: 'xs' },
    textProps: { size: 'xs', color: 'dimmed' }
};

export function GroupedSpotlightFooter(props: GroupedSpotlightFooterProps) {
    const {
        hints, hintProps, kbdProps, textProps, sx, ...others
    } = useComponentDefaultProps('GroupedSpotlightFooter', GroupedSpotlightFooterDefaultProps, props);

    if (!hints || hints.length === 0) return null;

    return (
        <Group
            sx={[
                theme => ({
                    flexShrink: 0,
                    borderTop: `1px solid ${theme.colorScheme === 'dark' ? theme.colors.dark[5] : theme.colors.gray[2]}`
                }),
                ...(Array.isArray(sx) ? sx : [sx])
            ]}
            {...others}
        >
            {hints.map((hint, index) => (
                <Group key={index} {...hintProps}>
                    {hint.keys.map(key => <Kbd key={key} {...kbdProps}>{key}</Kbd>)}
                    <Text {...textProps}>{hint.label}</Text>
                </Group>
            ))}
        </Group>
    );
}
