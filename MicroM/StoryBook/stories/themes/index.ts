import { MantineThemeOverride } from "@mantine/core";
import { exampleTheme } from "./example";

export interface StorybookTheme {
    id: string,
    name: string,
    theme: MantineThemeOverride,
}

export const EDITOR_THEME_ID = "editor";

export const themes: StorybookTheme[] = [
    { id: "mantine", name: "Mantine (default)", theme: {} },
    { id: "example", name: "MicroM example", theme: exampleTheme },
];

export function findTheme(id: string | undefined) {
    return themes.find(t => t.id === id);
}
