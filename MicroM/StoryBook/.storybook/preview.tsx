import { ColorScheme } from "@mantine/core";
import type { Decorator, Preview } from "@storybook/react-vite";
import { useGlobals } from "storybook/preview-api";
import { MicroMProviders, StorybookLocale } from "../stories/MicroMProviders";
import { useEditorTheme } from "../stories/theme-editor/themeStore";
import { EDITOR_THEME_ID, findTheme, themes } from "../stories/themes";
import "../stories/maps/googleMapsConfig";
import { ThemedDocsContainer } from "./ThemedDocsContainer";

function ThemedStory({ children, themeId, colorScheme, locale, gridStyles, initialRoute, onToggle }: {
    children: React.ReactNode, themeId: string, colorScheme: ColorScheme, locale: StorybookLocale, gridStyles: boolean,
    initialRoute?: string, onToggle: (v?: ColorScheme) => void
}) {
    const editorTheme = useEditorTheme();
    const theme = themeId === EDITOR_THEME_ID ? editorTheme : (findTheme(themeId)?.theme ?? {});
    return (
        <MicroMProviders theme={theme} colorScheme={colorScheme} locale={locale} withGridGlobalStyles={gridStyles} initialRoute={initialRoute} toggleColorScheme={onToggle}>
            {children}
        </MicroMProviders>
    );
}

const withMicroM: Decorator = (Story, context) => {
    const [globals, updateGlobals] = useGlobals();

    if (context.parameters.microm?.disableProviders) return <Story />;

    const colorScheme = (globals.colorScheme ?? "light") as ColorScheme;
    const themeId = (globals.mantineTheme ?? themes[0].id) as string;
    const locale = (globals.locale ?? "en-US") as StorybookLocale;
    const gridStyles = globals.gridStyles !== "off";
    const onToggle = (v?: ColorScheme) => updateGlobals({ colorScheme: v ?? (colorScheme === "dark" ? "light" : "dark") });

    return (
        <ThemedStory key={context.id} themeId={themeId} colorScheme={colorScheme} locale={locale} gridStyles={gridStyles}
            initialRoute={context.parameters.microm?.initialRoute} onToggle={onToggle}>
            <Story />
        </ThemedStory>
    );
};

const preview: Preview = {
    decorators: [withMicroM],
    globalTypes: {
        mantineTheme: {
            description: "Mantine theme",
            toolbar: {
                title: "Theme",
                icon: "paintbrush",
                items: [
                    ...themes.map(t => ({ value: t.id, title: t.name })),
                    { value: EDITOR_THEME_ID, title: "Theme Editor (localStorage)" },
                ],
                dynamicTitle: true,
            },
        },
        colorScheme: {
            description: "Color scheme",
            toolbar: {
                title: "Scheme",
                icon: "mirror",
                items: [
                    { value: "light", title: "Light", icon: "sun" },
                    { value: "dark", title: "Dark", icon: "moon" },
                ],
                dynamicTitle: true,
            },
        },
        locale: {
            description: "Format locale (dates, numbers)",
            toolbar: {
                title: "Locale",
                icon: "globe",
                items: [
                    { value: "en-US", title: "en-US" },
                    { value: "es-AR", title: "es-AR" },
                ],
                dynamicTitle: true,
            },
        },
        gridStyles: {
            description: "Mount GridGlobalStyles (theme-based Grid styles)",
            toolbar: {
                title: "Grid",
                icon: "grid",
                items: [
                    { value: "on", title: "GridGlobalStyles: on" },
                    { value: "off", title: "GridGlobalStyles: off (w2ui CSS only)" },
                ],
                dynamicTitle: true,
            },
        },
    },
    initialGlobals: {
        mantineTheme: themes[0].id,
        colorScheme: "light",
        locale: "en-US",
        gridStyles: "on",
    },
    parameters: {
        layout: "padded",
        docs: {
            container: ThemedDocsContainer,
        },
        controls: {
            matchers: { color: /(background|color)$/i, date: /Date$/i },
        },
        options: {
            storySort: {
                order: ["Introduction", "Theme Editor", "Mantine", "MicroM", "*"],
            },
        },
    },
};

export default preview;
