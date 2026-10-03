import { Box } from "@mantine/core";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { DeveloperToolsPanel } from "../../../micromlib/src/UI/DeveloperToolsPanel/DeveloperToolsPanel";
import { Customers } from "../mocks/entities";
import { createMockClient } from "../mocks/mockClient";

const meta = {
    title: "MicroM/Developer tools",
    parameters: { controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

// DeveloperToolsPanel replaces the entity source internally; the constructor is only required by its props type.
export const Panel: Story = {
    name: "DeveloperToolsPanel",
    render: () => (
        <Box h="75vh">
            <DeveloperToolsPanel client={createMockClient()} selectionMode="single" formMode="edit"
                entityConstructor={c => ({ entity: new Customers(c), view: "cus_brwStandard" })} />
        </Box>
    ),
};
