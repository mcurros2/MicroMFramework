import { Stack, StackProps, Text, TextProps, useComponentDefaultProps } from "@mantine/core";
import { IconSearch } from "@tabler/icons-react";
import { ReactNode } from "react";

export interface GroupedSpotlightNothingFoundProps extends Omit<StackProps, 'children' | 'title'> {
    query?: string,
    activeGroup?: string | null,
    icon?: ReactNode,
    title?: ReactNode | ((query: string, activeGroup: string | null) => ReactNode),
    description?: ReactNode,
    titleProps?: Omit<TextProps, 'children'>,
    descriptionProps?: Omit<TextProps, 'children'>
}

export const GroupedSpotlightNothingFoundDefaultProps: Partial<GroupedSpotlightNothingFoundProps> = {
    icon: <IconSearch size="2.75rem" stroke={1.2} />,
    title: query => `Nothing found for "${query}"`,
    description: 'Try a different search',
    align: 'center',
    justify: 'center',
    spacing: 6,
    mih: '12rem',
    titleProps: { fw: 700, fz: 'md', mt: 'xs' },
    descriptionProps: { fz: 'sm' }
};

export function GroupedSpotlightNothingFound(props: GroupedSpotlightNothingFoundProps) {
    const {
        query, activeGroup, icon, title, description, titleProps, descriptionProps, ...others
    } = useComponentDefaultProps('GroupedSpotlightNothingFound', GroupedSpotlightNothingFoundDefaultProps, props);

    return (
        <Stack {...others}>
            {icon}
            {title && <Text {...titleProps}>{typeof title === 'function' ? title((query ?? '').trim(), activeGroup ?? null) : title}</Text>}
            {description && <Text {...descriptionProps}>{description}</Text>}
        </Stack>
    );
}
