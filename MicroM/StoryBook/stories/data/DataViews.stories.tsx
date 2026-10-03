import { Box } from "@mantine/core";
import { DataView, DataViewPage, DataViewPanel } from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { useMemo } from "react";
import { CustomerCard } from "../mocks/CustomerCard";
import { Customers } from "../mocks/entities";
import { createMockClient } from "../mocks/mockClient";

interface DataViewArgs {
    selectionMode: "single" | "multi",
    itemsPerPage: number,
    enableAdd: boolean,
    enableEdit: boolean,
    enableDelete: boolean,
    showToolbar: boolean,
    formMode: "add" | "edit" | "view",
}

function DataViewStory(args: DataViewArgs) {
    const entity = useMemo(() => new Customers(createMockClient()), []);
    return <DataView entity={entity} viewName={entity.def.views.cus_brwStandard.name} Card={CustomerCard} {...args} />;
}

const meta = {
    title: "MicroM/Data/DataView",
    component: DataViewStory,
    args: { selectionMode: "multi", itemsPerPage: 12, enableAdd: true, enableEdit: true, enableDelete: true, showToolbar: true, formMode: "edit" },
    argTypes: {
        selectionMode: { control: "inline-radio", options: ["single", "multi"] },
        formMode: { control: "inline-radio", options: ["add", "edit", "view"] },
    },
} satisfies Meta<typeof DataViewStory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Cards: Story = {};

export const ReadOnly: Story = { name: "Read only", args: { formMode: "view", enableAdd: false, enableEdit: false, enableDelete: false } };

export const Page: Story = {
    name: "DataViewPage",
    render: (args) => {
        const client = createMockClient();
        return <DataViewPage client={client} entityConstructor={c => new Customers(c)} viewName="cus_brwStandard" Card={CustomerCard} {...args} />;
    },
};

export const Panel: Story = {
    name: "DataViewPanel",
    render: (args) => {
        const client = createMockClient();
        return (
            <Box h="80vh">
                <DataViewPanel client={client} {...args}
                    entityConstructor={c => { const e = new Customers(c); return { entity: e, view: e.def.views.cus_brwStandard.name, Card: CustomerCard }; }} />
            </Box>
        );
    },
};
