import { Box, Code, Group, Stack, Text } from "@mantine/core";
import { AutoFiltersForm, DataGridForm, DataGridPage, DataGridPanel, EntityActionButton } from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { useMemo, useState } from "react";
import { CustomerFilters, Customers } from "../mocks/entities";
import { createMockClient } from "../mocks/mockClient";

const meta = {
    title: "MicroM/Data/DataGrid variants",
    parameters: { controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const FiltersAndActions: Story = {
    name: "Filters and client actions",
    render: () => {
        const entity = new Customers(createMockClient());
        return (
            <Stack>
                <Text size="sm">
                    The filter button opens the view's FiltersEntity (AutoFiltersForm). Client actions (Approve, Newsletter) show because <Code>formMode</Code> is set.
                </Text>
                <DataGrid_ entity={entity} />
            </Stack>
        );
    },
};

function DataGrid_({ entity }: { entity: Customers }) {
    return <DataGridPanel client={entity.API.client} selectionMode="multi" formMode="edit" gridHeight="28rem"
        entityConstructor={() => ({ entity, view: entity.def.views.cus_brwStandard.name })} />;
}

export const Page: Story = {
    name: "DataGridPage",
    render: () => <DataGridPage client={createMockClient()} entityConstructor={c => new Customers(c)} viewName="cus_brwStandard" selectionMode="multi" formMode="edit" gridHeight="24rem" />,
};

export const Panel: Story = {
    name: "DataGridPanel",
    render: () => (
        <Box h="80vh">
            <DataGridPanel client={createMockClient()} selectionMode="multi" formMode="edit"
                entityConstructor={c => { const e = new Customers(c); return { entity: e, view: e.def.views.cus_brwStandard.name }; }} />
        </Box>
    ),
};

export const Form: Story = {
    name: "DataGridForm",
    render: () => {
        const entity = new Customers(createMockClient());
        return <DataGridForm entity={entity} viewName="cus_brwStandard" selectionMode="multi" formMode="edit" title="Customers" helpText="DataGridForm is meant to be embedded in an entity form." gridHeight="20rem" />;
    },
};

function ActionButtonStory() {
    const entity = useMemo(() => new Customers(createMockClient()), []);
    const [last, setLast] = useState("");
    return (
        <Stack>
            <Group>
                <EntityActionButton entity={entity} action={entity.def.clientActions.approve} selectedKeys={[{ c_customer_id: "1" }]}
                    onClose={async (r) => { setLast(`approve -> ${r}`); return true; }} />
                <EntityActionButton entity={entity} action={entity.def.clientActions.sendNewsletter} variant="light"
                    onClose={async (r) => { setLast(`newsletter -> ${r}`); return true; }} />
            </Group>
            <Text size="sm">Last result: <Code>{last || "(none)"}</Code></Text>
        </Stack>
    );
}

export const ActionButton: Story = { name: "EntityActionButton", render: () => <ActionButtonStory /> };

export const FiltersForm: Story = {
    name: "AutoFiltersForm",
    render: () => <AutoFiltersForm entity={new CustomerFilters(createMockClient())} initialFormMode="edit" />,
};
