import { Anchor, Card, CardProps, CSSObject, DefaultMantineColor, DefaultProps, Group, MantineNumberSize, MantineSize, packSx, Stack, Text, Title, TitleOrder, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { ComponentType, PropsWithChildren, ReactNode } from "react";
import { Fragment } from "react/jsx-runtime";
import { MenuItem } from "../../Menu/MenuItem";
import { navigateToRoute, normalizeRouteURL } from "../../Router/MicroMRouterState";

export interface MenuCardItemProps<TContainerProps extends DefaultProps> extends Omit<CardProps, 'children'> {
    item: MenuItem,
    routeBuilder?: (path: string) => string,
    hoverStyles?: CSSObject,
    titleOrder?: TitleOrder,
    titleColorLight?: DefaultMantineColor,
    titleColorDark?: DefaultMantineColor,
    headerSpacing?: MantineNumberSize,
    shortcutSpacing?: MantineNumberSize,
    shortcutSize?: MantineSize,
    shortcutSeparator?: ReactNode,
    noIconIndent?: MantineNumberSize,
    showDescription?: boolean,
    SubitemsContainer?: ComponentType<PropsWithChildren & TContainerProps>
    SubitemsContainerProps?: Partial<TContainerProps>,
    subitemsDisplayMode?: 'grid' | 'group' | 'custom',
    shortcutAnchorProps?: Omit<React.ComponentPropsWithoutRef<typeof Anchor>, 'href' | 'size'>,
    shortcutColumnsThreshold?: number,
}

export const MenuCardItemDefaultProps: Partial<MenuCardItemProps<any>> = {
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
    SubitemsContainer: Group,
};

export function MenuCardItem<TContainerProps extends DefaultProps>(props: MenuCardItemProps<TContainerProps>) {
    const {
        item, routeBuilder, hoverStyles, titleOrder, titleColorLight, titleColorDark, headerSpacing,
        shortcutSpacing, shortcutSize, shortcutSeparator, noIconIndent, sx, style, showDescription,
        SubitemsContainer, SubitemsContainerProps, shortcutAnchorProps, ...others
    } = useComponentDefaultProps('MenuCardItem', MenuCardItemDefaultProps, props);

    const theme = useMantineTheme();

    const subitemsContainerProps = {
        ...SubitemsContainerProps,
        ...(shortcutSpacing !== undefined ?{ spacing: shortcutSpacing } : {}),
    } as PropsWithChildren & TContainerProps;

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
                {showDescription && item.description && (
                    <Text size="sm" color="dimmed">
                        {item.description}
                    </Text>
                )}
                {SubitemsContainer && 
                    <SubitemsContainer {...subitemsContainerProps} key={`cig${item.ID}`}>
                        {item.subitems?.filter((s) => s.canShowAsShortcut)
                            .map((subitem, index, arr) => (
                                <Fragment key={`sh${subitem.ID}`}>
                                    <Anchor
                                        { ...shortcutAnchorProps }
                                        href={normalizeRouteURL(routeBuilder!(subitem.menuPath || ''))}
                                        size={shortcutSize}
                                    >
                                        {subitem.label}
                                    </Anchor>
                                    {index < arr.length - 1 && shortcutSeparator}
                                </Fragment>
                            ))}
                    </SubitemsContainer>
                }
            </Stack>
        </Card>
    )
}
