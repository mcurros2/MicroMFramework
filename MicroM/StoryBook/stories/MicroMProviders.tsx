import { ColorScheme, ColorSchemeProvider, MantineProvider, MantineThemeOverride } from "@mantine/core";
import { DatesProvider } from "@mantine/dates";
import { GridGlobalStyles, MicroMRouter, ModalsManager } from "@mcurros2/microm";
import "dayjs/locale/es";
import { PropsWithChildren, useEffect, useMemo, useState } from "react";

export type StorybookLocale = "en-US" | "es-AR";

export interface MicroMProvidersProps extends PropsWithChildren {
    theme: MantineThemeOverride,
    colorScheme: ColorScheme,
    toggleColorScheme?: (value?: ColorScheme) => void,
    locale?: StorybookLocale,
    withGlobalStyles?: boolean,
    // micromlib exports GridGlobalStyles but never mounts it; apps must include it for the Grid to follow the theme.
    withGridGlobalStyles?: boolean,
    initialRoute?: string,
}

// MicroMRouter reads location.hash only on mount, so each story starts from its own route.
// Links rendered as href="/#/..." would navigate the Storybook iframe away, so they are turned into hash changes.
function RouterSandbox({ initialRoute = "", children }: PropsWithChildren<{ initialRoute?: string }>) {
    useState(() => {
        window.history.replaceState(window.history.state, "", `${window.location.pathname}${window.location.search}${initialRoute ? `#${initialRoute}` : ""}`);
        return 0;
    });

    useEffect(() => {
        const onClick = (e: MouseEvent) => {
            if (e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
            const anchor = (e.target as Element | null)?.closest?.('a[href^="/#/"]');
            if (!anchor) return;
            e.preventDefault();
            window.location.hash = anchor.getAttribute("href")!.slice(2);
        };
        document.addEventListener("click", onClick, true);
        return () => document.removeEventListener("click", onClick, true);
    }, []);

    return <MicroMRouter>{children}</MicroMRouter>;
}

export function MicroMProviders({
    theme, colorScheme, toggleColorScheme, locale = "en-US", withGlobalStyles = true, withGridGlobalStyles = true, initialRoute, children
}: MicroMProvidersProps) {
    const resolvedTheme = useMemo<MantineThemeOverride>(() => ({
        ...theme,
        colorScheme,
        components: {
            useLocaleFormat: { defaultProps: { initialLocale: locale } },
            ...theme.components,
        },
    }), [theme, colorScheme, locale]);

    return (
        <ColorSchemeProvider colorScheme={colorScheme} toggleColorScheme={toggleColorScheme ?? (() => { })}>
            <MantineProvider theme={resolvedTheme} withGlobalStyles={withGlobalStyles} withNormalizeCSS={withGlobalStyles}>
                {withGridGlobalStyles && <GridGlobalStyles />}
                <DatesProvider settings={{ locale: locale === "es-AR" ? "es" : "en", firstDayOfWeek: locale === "es-AR" ? 1 : 0 }}>
                    <ModalsManager animationDuration={0} modalProps={{}}>
                        <RouterSandbox initialRoute={initialRoute}>
                            {children}
                        </RouterSandbox>
                    </ModalsManager>
                </DatesProvider>
            </MantineProvider>
        </ColorSchemeProvider>
    );
}
