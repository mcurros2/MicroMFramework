import { DefaultMantineColor, Group, MantineNumberSize, Text, ThemeIcon, Tooltip, TooltipProps, UnstyledButton, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { ReactNode } from "react";
import { NavBarMode } from "./MenuCardNavBarPanel";
import { NavBarThemeFunctions, NavBarThemeFunctionsDefaults, NavBarVariantStyles, NavBarVariantStylesDefaults } from "./navBarTheme";

export interface MenuCardNavBarItemProps {
    icon: ReactNode,
    label: string,
    active: boolean,
    mode: NavBarMode,
    href?: string,
    onClick?: () => void,
    radius?: MantineNumberSize,
    mb?: MantineNumberSize,
    brandColor?: DefaultMantineColor,
    textColor?: DefaultMantineColor,
    /** Size of the icon container */
    iconSize?: MantineNumberSize,
    /** Props for the tooltip shown in icons mode. Replaces the default object entirely when set per instance */
    tooltipProps?: Omit<TooltipProps, 'label' | 'children'>,
    /** Shades and scales used to style the item. Replaces the default object entirely — spread NavBarVariantStylesDefaults to override selectively */
    variantStyles?: NavBarVariantStyles,
    /** Style functions for the item. Replaces the default object entirely — spread NavBarThemeFunctionsDefaults to override selectively */
    themeFunctions?: NavBarThemeFunctions,
}

export const MenuCardNavBarItemDefaultProps: Partial<MenuCardNavBarItemProps> = {
    radius: 'md',
    textColor: 'gray',
    iconSize: 'lg',
    tooltipProps: { withArrow: true, position: 'right' },
    variantStyles: NavBarVariantStylesDefaults,
    themeFunctions: NavBarThemeFunctionsDefaults,
};

export function MenuCardNavBarItem(props: MenuCardNavBarItemProps) {
    const { icon, label, active, mode, href, onClick, radius, mb, brandColor, textColor, iconSize, tooltipProps, variantStyles, themeFunctions } =
        useComponentDefaultProps('MenuCardNavBarItem', MenuCardNavBarItemDefaultProps, props);
    const theme = useMantineTheme();
    const fns = themeFunctions!;
    const linkProps = href ? { component: 'a' as const, href } : {};
    const labelColor = theme.colors[textColor!][variantStyles!.labelShade];

    if (mode === 'icons') {
        return (
            <Tooltip label={label} {...tooltipProps}>
                <ThemeIcon
                    {...linkProps}
                    color={fns.getNavBarButtonsColor(theme, brandColor)} variant="transparent" size={iconSize} radius={radius} mb={mb}
                    onClick={onClick}
                    sx={{
                        color: labelColor,
                        ...fns.getNavBarIconItemStyles(theme, active, brandColor, variantStyles),
                    }}
                >
                    {icon}
                </ThemeIcon>
            </Tooltip>
        );
    }

    return (
        <UnstyledButton onClick={onClick} w="100%" mb={mb}
            sx={fns.getNavBarTextItemStyles(theme, active, brandColor, variantStyles)}
            {...linkProps}
        >
            <Group noWrap>
                <ThemeIcon
                    className="menu-card-navbar-item-icon"
                    color={fns.getNavBarButtonsColor(theme, brandColor)} variant="transparent" size={iconSize} radius={radius}
                    sx={{
                        color: labelColor,
                        ...fns.getNavBarItemActiveStyles(theme, active, brandColor, variantStyles),
                    }}
                >
                    {icon}
                </ThemeIcon>
                <Text className="menu-card-navbar-item-label"
                    style={{ color: labelColor }}>
                    {label}
                </Text>
            </Group>
        </UnstyledButton>
    )
}
