import { ColorScheme } from "@mantine/core";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { ThemeEditor } from "./ThemeEditor";

const meta = {
    title: "Theme Editor",
    component: ThemeEditor,
    parameters: {
        layout: "fullscreen",
        microm: { disableProviders: true },
        controls: { disable: true },
    },
} satisfies Meta<typeof ThemeEditor>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Editor: Story = {
    render: (_args, context) => <ThemeEditor initialColorScheme={(context.globals.colorScheme ?? "light") as ColorScheme} locale={context.globals.locale ?? "en-US"} />,
};
