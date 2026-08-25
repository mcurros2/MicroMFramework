import { Anchor, Card, CardProps, CSSObject, DefaultMantineColor, Group, MantineNumberSize, MantineSize, packSx, Stack, Text, Title, TitleOrder, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { ReactNode } from "react";
import { Fragment } from "react/jsx-runtime";
import { MenuItem } from "../../Menu/MenuItem";
import { navigateToRoute, normalizeRouteURL } from "../../Router/MicroMRouterState";

export interface MenuCardItemProps extends Omit<CardProps, 'children'> {
    item: MenuItem,
    routeBuilder?: (path: string) => string,
    /** Styles applied to the card on hover */
    hoverStyles?: CSSObject,
    /** Heading order for the item title */
    titleOrder?: TitleOrder,
    /** Title color in light color scheme */
    titleColorLight?: DefaultMantineColor,
    /** Title color in dark color scheme */
    titleColorDark?: DefaultMantineColor,
    /** Spacing between the icon and the title */
    headerSpacing?: MantineNumberSize,
    /** Spacing between shortcut links */
    shortcutSpacing?: MantineNumberSize,
    /** Font size of shortcut links */
    shortcutSize?: MantineSize,
    /** Node rendered between shortcut links */
    shortcutSeparator?: ReactNode,
    /** Left indent applied to shortcuts when the item has no icon */
    noIconIndent?: MantineNumberSize,
}

export const MenuCardItemDefaultProps: Partial<MenuCardItemProps> = {
    routeBuilder: path => path,
    shadow: 'sm',
    withBorder: true,
    hoverStyles: { transform: 'scale(1.02)', boxShadow: 'var(--mantine-shadow-md)' },
    titleOrder: 4,
    titleColorLight: 'gray.7',
    titleColorDark: undefined,
    headerSpacing: 'xs',
    shortcutSpacing: '0.3rem',
    shortcutSize: 'sm',
    shortcutSeparator: <Text size="xs" color="dimmed">·</Text>,
    noIconIndent: '2.1rem',
};

export function MenuCardItem(props: MenuCardItemProps) {
    const {
        item, routeBuilder, hoverStyles, titleOrder, titleColorLight, titleColorDark, headerSpacing,
        shortcutSpacing, shortcutSize, shortcutSeparator, noIconIndent, sx, style, ...others
    } = useComponentDefaultProps('MenuCardItem', MenuCardItemDefaultProps, props);
    const theme = useMantineTheme();

    return (
        <Card
            key={`cir${item.ID}`}
            {...others}
            onClick={() => navigateToRoute(routeBuilder!(item.menuPath || ''))}
            style={{ cursor: 'pointer', ...style }}
            sx={[{ '&:hover': hoverStyles }, ...packSx(sx)]}
        >
            <Group spacing={headerSpacing} align="flex-start">
                {item.icon}
                <Title order={titleOrder} color={theme.colorScheme === 'light' ? titleColorLight : titleColorDark}>{item.label}</Title>
            </Group>
            <Stack spacing="xs" mt="xs" ml={!item.icon ? noIconIndent : undefined} >
                <Group key={`cig${item.ID}`} spacing={shortcutSpacing}>
                    {item.subitems?.filter((s) => s.canShowAsShortcut)
                        .map((subitem, index, arr) => (
                            <Fragment key={`sh${subitem.ID}`}>
                                <Anchor
                                    href={normalizeRouteURL(routeBuilder!(subitem.menuPath || ''))}
                                    size={shortcutSize}
                                >
                                    {subitem.label}
                                </Anchor>
                                {index < arr.length - 1 && shortcutSeparator}
                            </Fragment>
                        ))}
                </Group>
            </Stack>
        </Card>
    )
}
