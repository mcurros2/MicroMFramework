import { DefaultMantineColor, Navbar, NavbarProps, Stack, Tooltip, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { useSpotlight } from "@mantine/spotlight";
import { IconHome, IconSearch } from "@tabler/icons-react";
import { ReactNode } from "react";
import { MenuItem } from "../../Menu/MenuItem";
import { parseMenuRoute } from "../../Router/MenuRoute";
import { normalizeRouteURL } from "../../Router/MicroMRouterState";
import { useMicroMRouter } from "../../Router/useMicroMRouter";
import { MenuCardNavBarItem, MenuCardNavBarItemProps } from "./MenuCardNavBarItem";
import { NavBarThemeFunctions, NavBarThemeFunctionsDefaults, NavBarVariantStyles, NavBarVariantStylesDefaults } from "./navBarTheme";

export type NavBarMode = 'icons' | 'text';
export type MenuMode = NavBarMode | 'collapsed';

type NavBarItemOverrides = Partial<Pick<MenuCardNavBarItemProps, 'iconSize' | 'tooltipProps' | 'radius' | 'textColor' | 'mb'>>;

export interface MenuCardNavBarPanelProps extends Omit<NavbarProps, 'children' | 'width'> {
    isLoggedIn?: boolean,
    rootItems: MenuItem[],
    Logo?: ReactNode,
    logoTooltip?: string,
    mode: NavBarMode,
    homeMenuId: string,
    homeLabel?: string,
    searchLabel?: string,
    brandColor?: DefaultMantineColor,
    /** Navbar width in icons mode */
    widthIcons?: number,
    /** Navbar width in text mode */
    widthText?: number,
    /** Icon for the home item */
    homeIcon?: ReactNode,
    /** Icon for the search item */
    searchIcon?: ReactNode,
    /** Shades and scales used to style the navbar and its items. Replaces the default object entirely — spread NavBarVariantStylesDefaults to override selectively */
    variantStyles?: NavBarVariantStyles,
    /** Style functions for the navbar and its items. Replaces the default object entirely — spread NavBarThemeFunctionsDefaults to override selectively */
    themeFunctions?: NavBarThemeFunctions,
    /** Overrides applied to every navbar item. Replaces the default object entirely when set per instance */
    itemProps?: NavBarItemOverrides,
    /** Overrides applied to the search item only, after itemProps. Replaces the default object entirely when set per instance */
    searchItemProps?: NavBarItemOverrides,
}

export const MenuCardNavBarPanelDefaultProps: Partial<MenuCardNavBarPanelProps> = {
    logoTooltip: '',
    homeLabel: 'Home',
    searchLabel: 'Search the menu',
    widthIcons: 60,
    widthText: 240,
    homeIcon: <IconHome />,
    searchIcon: <IconSearch size="1.3rem" />,
    searchItemProps: { radius: 'xl', mb: 'xs' },
    variantStyles: NavBarVariantStylesDefaults,
    themeFunctions: NavBarThemeFunctionsDefaults,
    p: 'xs',
    pt: '0.2rem',
    pl: '0.845rem',
};

export function MenuCardNavBarPanel(props: MenuCardNavBarPanelProps) {
    const {
        isLoggedIn, rootItems, Logo, logoTooltip, mode, homeMenuId, homeLabel, searchLabel, brandColor,
        widthIcons, widthText, homeIcon, searchIcon, variantStyles, themeFunctions, itemProps, searchItemProps, bg, ...others
    } = useComponentDefaultProps('MenuCardNavBarPanel', MenuCardNavBarPanelDefaultProps, props);

    const theme = useMantineTheme();
    const fns = themeFunctions!;

    const search = useSpotlight();

    const { path, route } = useMicroMRouter();

    const menuRoute = parseMenuRoute(route);
    const activePath = menuRoute?.context?.originPath ?? path;
    const rootPath = activePath.split('/').filter(Boolean)[1];
    const homePath = `/${homeMenuId}`;

    return (
        <Navbar
            width={{ base: mode === 'text' ? widthText! : widthIcons! }}
            bg={bg ?? fns.getNavBarBackgroundColor(theme, brandColor, variantStyles)}
            {...others}
        >
            {Logo && (
                <Navbar.Section>
                    {mode === 'icons'
                        ? <Tooltip withArrow withinPortal={true} label={logoTooltip} position="right">{Logo}</Tooltip>
                        : Logo
                    }
                </Navbar.Section>
            )}
            <Navbar.Section grow mt="md">
                <Stack>
                    <MenuCardNavBarItem
                        mode={mode}
                        icon={searchIcon}
                        label={searchLabel!}
                        active={false}
                        onClick={() => search.openSpotlight()}
                        brandColor={brandColor}
                        variantStyles={variantStyles}
                        themeFunctions={themeFunctions}
                        {...itemProps}
                        {...searchItemProps}
                    />
                    <MenuCardNavBarItem
                        mode={mode}
                        icon={homeIcon}
                        label={homeLabel!}
                        active={activePath === homePath}
                        href={normalizeRouteURL(homePath)}
                        brandColor={brandColor}
                        variantStyles={variantStyles}
                        themeFunctions={themeFunctions}
                        {...itemProps}
                    />
                    {isLoggedIn && rootItems.map((item) => (
                        <MenuCardNavBarItem
                            key={item.ID}
                            mode={mode}
                            icon={item.icon}
                            label={item.label}
                            active={rootPath === item.ID}
                            href={normalizeRouteURL(item.menuPath || '')}
                            brandColor={brandColor}
                            variantStyles={variantStyles}
                            themeFunctions={themeFunctions}
                            {...itemProps}
                        />
                    ))}
                </Stack>
            </Navbar.Section>
        </Navbar>
    )
}
