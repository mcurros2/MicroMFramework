import { Anchor, Card, CardProps, CSSObject, DefaultMantineColor, DefaultProps, Group, MantineNumberSize, MantineSize, packSx, SimpleGrid, SimpleGridProps, Stack, Text, Title, TitleOrder, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { ComponentType, Fragment, PropsWithChildren, ReactNode } from "react";
import { MenuItem } from "../../Menu/MenuItem";
import { navigateToRoute, normalizeRouteURL } from "../../Router/MicroMRouterState";


export type SubitemsDisplayMode = 'group' | 'grid' | 'custom';

export type MenuCardItemGridContainerProps = Omit<SimpleGridProps, 'children'>;

interface MenuCardItemBaseProps extends Omit<CardProps, 'children'> {
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
    shortcutAnchorProps?: Omit<React.ComponentPropsWithoutRef<typeof Anchor>, 'href' | 'size'>,
}

interface SubitemsGroupModeProps {
    subitemsDisplayMode?: 'group',
    gridContainerProps?: never,
    shortcutColumnsThreshold?: never,
    SubitemsContainer?: never,
    SubitemsContainerProps?: never,
}

interface SubitemsGridModeProps {
    subitemsDisplayMode: 'grid',
    gridContainerProps?: MenuCardItemGridContainerProps,
    shortcutColumnsThreshold?: number,
    SubitemsContainer?: never,
    SubitemsContainerProps?: never,
}

interface SubitemsCustomModeProps<TContainerProps extends DefaultProps> {
    subitemsDisplayMode: 'custom',
    SubitemsContainer: ComponentType<PropsWithChildren & TContainerProps>,
    SubitemsContainerProps?: Partial<TContainerProps>,
    gridContainerProps?: never,
    shortcutColumnsThreshold?: never,
}

export type MenuCardItemProps<TContainerProps extends DefaultProps = DefaultProps> = MenuCardItemBaseProps & (
    SubitemsGroupModeProps | SubitemsGridModeProps | SubitemsCustomModeProps<TContainerProps>
);


export interface MenuCardItemResolvedProps extends MenuCardItemBaseProps {
    subitemsDisplayMode?: SubitemsDisplayMode,
    gridContainerProps?: MenuCardItemGridContainerProps,
    shortcutColumnsThreshold?: number,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    SubitemsContainer?: ComponentType<any>,
    SubitemsContainerProps?: Record<string, unknown>,
}

export const MenuCardItemDefaultProps: Partial<MenuCardItemResolvedProps> = {
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
    subitemsDisplayMode: 'group',
    gridContainerProps: {
        cols: 2,
        spacing: 'md',
        verticalSpacing: '0.3rem',
        breakpoints: [
            { maxWidth: '30rem', cols: 1 },
        ],
    },
};

export function MenuCardItem<TContainerProps extends DefaultProps = DefaultProps>(props: MenuCardItemProps<TContainerProps>) {
    const {
        item, routeBuilder, hoverStyles, titleOrder, titleColorLight, titleColorDark, headerSpacing,
        shortcutSpacing, shortcutSize, shortcutSeparator, noIconIndent, sx, style, showDescription,
        subitemsDisplayMode, gridContainerProps, shortcutColumnsThreshold, SubitemsContainer, SubitemsContainerProps,
        shortcutAnchorProps, ...others
    } = useComponentDefaultProps('MenuCardItem', MenuCardItemDefaultProps, props as MenuCardItemResolvedProps);

    const theme = useMantineTheme();

    const shortcuts = item.subitems?.filter((s) => s.canShowAsShortcut) ?? [];

    const renderAnchor = (subitem: MenuItem) => (
        <Anchor
            {...shortcutAnchorProps}
            href={normalizeRouteURL(routeBuilder!(subitem.menuPath || ''))}
            size={shortcutSize}
        >
            {subitem.label}
        </Anchor>
    );

    // inline and custom containers: shortcuts separated by shortcutSeparator
    const renderSeparated = () => shortcuts.map((subitem, index) => (
        <Fragment key={`sh${subitem.ID}`}>
            {renderAnchor(subitem)}
            {index < shortcuts.length - 1 && shortcutSeparator}
        </Fragment>
    ));

    const renderSubitems = () => {

        if (subitemsDisplayMode === 'grid') {
            // one cell per shortcut, no separator
            return shortcutColumnsThreshold === undefined || shortcuts.length > shortcutColumnsThreshold ? 
                <SimpleGrid {...gridContainerProps}>
                    {shortcuts.map((subitem) => <Fragment key={`sh${subitem.ID}`}>{renderAnchor(subitem)}</Fragment>)}
                </SimpleGrid>
                : <Stack spacing={shortcutSpacing}>
                    {shortcuts.map((subitem) => <Fragment key={`sh${subitem.ID}`}>{renderAnchor(subitem)}</Fragment>)}
                </Stack>;
        }

        if (subitemsDisplayMode === 'custom') {
            if (!SubitemsContainer) return null;
            const containerProps = {
                ...(shortcutSpacing !== undefined ? { spacing: shortcutSpacing } : {}),
                ...SubitemsContainerProps,
            };
            return <SubitemsContainer {...containerProps}>{renderSeparated()}</SubitemsContainer>;
        }

        return <Group spacing={shortcutSpacing}>{renderSeparated()}</Group>;
    };

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
                {renderSubitems()}
            </Stack>
        </Card>
    )
}
