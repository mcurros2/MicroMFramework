import { MantineThemeOverride } from "@mantine/core";

export const exampleTheme: MantineThemeOverride = {
    primaryColor: "brand",
    primaryShade: { light: 6, dark: 5 },
    colors: {
        brand: ["#e6f4f8", "#c4e4ee", "#9dd2e3", "#72bed7", "#4eadcd", "#2f9cc2", "#1f86a8", "#186a86", "#124f64", "#0b3442"],
    },
    fontFamily: "Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif",
    headings: { fontFamily: "Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif", fontWeight: 700 },
    defaultRadius: "md",
    components: {
        EntityForm: { defaultProps: { okButtonVariant: "filled", cancelButtonVariant: "default" } },
        DataGrid: { defaultProps: { toolbarIconVariant: "subtle", actionsButtonVariant: "light" } },
        Button: { defaultProps: { radius: "md" } },
    },
};
