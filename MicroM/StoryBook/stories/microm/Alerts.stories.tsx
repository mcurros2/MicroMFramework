import { Stack } from "@mantine/core";
import { AlertError, AlertInfo, AlertSuccess } from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";

const meta = {
    title: "MicroM/Core/Alerts",
    component: AlertInfo,
    tags: ["autodocs"],
    args: {
        title: "Information",
        children: "This is a MicroM message. Colors come from the Mantine theme.",
        variant: "light",
        withCloseButton: false,
    },
    argTypes: {
        variant: { control: "inline-radio", options: ["light", "filled", "outline"] },
    },
} satisfies Meta<typeof AlertInfo>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Info: Story = {};

export const All: Story = {
    render: (args) => (
        <Stack>
            <AlertInfo {...args} title="Information">Operation in progress. It may take a few seconds.</AlertInfo>
            <AlertSuccess {...args} title="Done">Data saved successfully.</AlertSuccess>
            <AlertError {...args} title="Error">Could not connect to the server.</AlertError>
        </Stack>
    ),
};
