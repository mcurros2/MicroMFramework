import { CSSObject, DefaultMantineColor, MantineTheme, rem } from "@mantine/core";

/**
 * Shades and scales used by the navbar style functions. To override only some
 * values, spread the defaults: `{ ...NavBarVariantStylesDefaults, hoverScale: 1.5 }`.
 */
export interface NavBarVariantStyles {
    /** Color shade (0-9) for the navbar background in light color scheme */
    backgroundShade: number,
    /** Focus ring width */
    focusOutlineWidth: string,
    /** Color shade (0-9) for the focus ring */
    focusOutlineShade: number,
    /** Focus ring offset */
    focusOutlineOffset: string,
    /** Scale applied to an item on hover (icons mode) */
    hoverScale: number,
    /** Scale applied to the active item */
    activeScale: number,
    /** Color shade (0-9) for the active item background */
    activeBackgroundShade: number,
    /** Scale applied to the item icon on hover (text mode) */
    hoverIconScale: number,
    /** Color shade (0-9) for the icon background on hover (text mode) */
    hoverIconBackgroundShade: number,
    /** Color shade (0-9) for the row background on hover/active (text mode) */
    hoverBackgroundShade: number,
    /** Color shade (0-9) for the item label and icon color */
    labelShade: number,
}

export const NavBarVariantStylesDefaults: NavBarVariantStyles = {
    backgroundShade: 8,
    focusOutlineWidth: rem(2),
    focusOutlineShade: 6,
    focusOutlineOffset: rem(2),
    hoverScale: 1.2,
    activeScale: 1.2,
    activeBackgroundShade: 4,
    hoverIconScale: 1.2,
    hoverIconBackgroundShade: 4,
    hoverBackgroundShade: 7,
    labelShade: 3,
};

export function getNavBarButtonsColor(theme: MantineTheme, brandColor?: DefaultMantineColor): DefaultMantineColor {
    return brandColor ?? theme.primaryColor;
}

export function getNavBarBackgroundColor(theme: MantineTheme, brandColor?: DefaultMantineColor, styles: NavBarVariantStyles = NavBarVariantStylesDefaults) {
    if (theme.colorScheme !== 'light') return undefined;
    return theme.colors[getNavBarButtonsColor(theme, brandColor)][styles.backgroundShade];
}

function getNavBarFocusStyles(theme: MantineTheme, brandColor: DefaultMantineColor | undefined, styles: NavBarVariantStyles) {
    const color = getNavBarButtonsColor(theme, brandColor);
    return {
        '&:focus-visible': {
            outline: `${styles.focusOutlineWidth} solid ${theme.colors[color][styles.focusOutlineShade]}`,
            outlineOffset: styles.focusOutlineOffset,
        },
        '&:focus:not(:focus-visible)': { outline: 'none' },
    };
}

function getNavBarHoverStyles(styles: NavBarVariantStyles) {
    return { '&:hover': { transform: `scale(${styles.hoverScale})` } };
}

export function getNavBarItemActiveStyles(theme: MantineTheme, active: boolean, brandColor?: DefaultMantineColor, styles: NavBarVariantStyles = NavBarVariantStylesDefaults) {
    if (!active) return {};

    const color = getNavBarButtonsColor(theme, brandColor);
    return {
        transform: `scale(${styles.activeScale})`,
        backgroundColor: theme.colors[color][styles.activeBackgroundShade],
        '&:hover': { backgroundColor: theme.colors[color][styles.activeBackgroundShade] },
    };
}

export function getNavBarIconItemStyles(theme: MantineTheme, active: boolean, brandColor?: DefaultMantineColor, styles: NavBarVariantStyles = NavBarVariantStylesDefaults) {
    return {
        ...getNavBarFocusStyles(theme, brandColor, styles),
        ...getNavBarHoverStyles(styles),
        ...getNavBarItemActiveStyles(theme, active, brandColor, styles),
    };
}

export function getNavBarTextItemStyles(theme: MantineTheme, active: boolean, brandColor?: DefaultMantineColor, styles: NavBarVariantStyles = NavBarVariantStylesDefaults) {
    const color = getNavBarButtonsColor(theme, brandColor);
    return {
        '&:hover .menu-card-navbar-item-icon': { transform: `scale(${styles.hoverIconScale})`, backgroundColor: theme.colors[color][styles.hoverIconBackgroundShade] },
        '&:hover': { backgroundColor: theme.colors[color][styles.hoverBackgroundShade] },
        ...(active ? { backgroundColor: theme.colors[color][styles.hoverBackgroundShade] } : {}),
        ...getNavBarFocusStyles(theme, brandColor, styles),
    };
}

/**
 * Style functions used by the navbar components. Provide your own implementations
 * to fully replace the styling logic. To replace only some functions, spread the
 * defaults: `{ ...NavBarThemeFunctionsDefaults, getNavBarBackgroundColor: mine }`.
 * The `styles` argument arrives already resolved (a complete NavBarVariantStyles).
 * Note: the composite functions (getNavBarIconItemStyles / getNavBarTextItemStyles)
 * are self-contained — overriding getNavBarItemActiveStyles does not change what
 * the default composites produce.
 */
export interface NavBarThemeFunctions {
    /** Resolves the base color used by the navbar buttons */
    getNavBarButtonsColor: (theme: MantineTheme, brandColor?: DefaultMantineColor) => DefaultMantineColor,
    /** Navbar background color; return undefined to keep the color scheme default */
    getNavBarBackgroundColor: (theme: MantineTheme, brandColor?: DefaultMantineColor, styles?: NavBarVariantStyles) => DefaultMantineColor | undefined,
    /** Styles applied to the active item icon */
    getNavBarItemActiveStyles: (theme: MantineTheme, active: boolean, brandColor?: DefaultMantineColor, styles?: NavBarVariantStyles) => CSSObject,
    /** Styles for an item in icons mode (focus + hover + active) */
    getNavBarIconItemStyles: (theme: MantineTheme, active: boolean, brandColor?: DefaultMantineColor, styles?: NavBarVariantStyles) => CSSObject,
    /** Styles for an item row in text mode (hover + active + focus) */
    getNavBarTextItemStyles: (theme: MantineTheme, active: boolean, brandColor?: DefaultMantineColor, styles?: NavBarVariantStyles) => CSSObject,
}

export const NavBarThemeFunctionsDefaults: NavBarThemeFunctions = {
    getNavBarButtonsColor,
    getNavBarBackgroundColor,
    getNavBarItemActiveStyles,
    getNavBarIconItemStyles,
    getNavBarTextItemStyles,
};
