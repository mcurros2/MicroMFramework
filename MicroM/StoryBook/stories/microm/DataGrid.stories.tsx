import { DataGrid } from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { useMemo } from "react";
import { Categories as CategoriesEntity, Customers as CustomersEntity, CustomersAutoForm } from "../mocks/entities";
import { createMockClient } from "../mocks/mockClient";

type Variant = "filled" | "light" | "outline" | "subtle" | "default" | "transparent";
type ButtonVariant = "filled" | "light" | "outline" | "subtle" | "default" | "white" | "gradient";

interface DataGridArgs {
    entityName: "Customers" | "Customers (AutoForm)" | "Categories",
    selectionMode: "single" | "multi",
    toolbarIconVariant: Variant,
    actionsButtonVariant: ButtonVariant,
    toolbarSize: "xs" | "sm" | "md" | "lg" | "xl",
    columnBorders: boolean,
    rowBorders: boolean,
    withBorder: boolean,
    showToolbar: boolean,
    showActionsToolbar: boolean,
    enableAdd: boolean,
    enableEdit: boolean,
    enableDelete: boolean,
    enableExport: boolean,
    gridHeight: string,
    latency: number,
}

function DataGridStory({ entityName, latency, ...props }: DataGridArgs) {
    const entity = useMemo(() => {
        const client = createMockClient({ latency });
        if (entityName === "Categories") return new CategoriesEntity(client);
        if (entityName === "Customers (AutoForm)") return new CustomersAutoForm(client);
        return new CustomersEntity(client);
    }, [entityName, latency]);

    const viewName = entity.def.standardView() ?? "";

    return <DataGrid key={`${entityName}${latency}`} entity={entity} viewName={viewName} {...props} />;
}

const meta = {
    title: "MicroM/Data/DataGrid",
    component: DataGridStory,
    args: {
        entityName: "Customers",
        selectionMode: "multi",
        toolbarIconVariant: "light",
        actionsButtonVariant: "light",
        toolbarSize: "sm",
        columnBorders: true,
        rowBorders: false,
        withBorder: true,
        showToolbar: true,
        showActionsToolbar: true,
        enableAdd: true,
        enableEdit: true,
        enableDelete: true,
        enableExport: true,
        gridHeight: "28rem",
        latency: 350,
    },
    argTypes: {
        entityName: { control: "inline-radio", options: ["Customers", "Customers (AutoForm)", "Categories"] },
        selectionMode: { control: "inline-radio", options: ["single", "multi"] },
        toolbarIconVariant: { control: "select", options: ["filled", "light", "outline", "subtle", "default", "transparent"] },
        actionsButtonVariant: { control: "select", options: ["filled", "light", "outline", "subtle", "default", "white", "gradient"] },
        toolbarSize: { control: "inline-radio", options: ["xs", "sm", "md", "lg", "xl"] },
        latency: { control: { type: "range", min: 0, max: 3000, step: 50 }, description: "Simulated backend latency (ms)" },
    },
} satisfies Meta<typeof DataGridStory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Customers: Story = {};

export const WithAutoForm: Story = { name: "Customers with AutoForm", args: { entityName: "Customers (AutoForm)" } };

export const Categories: Story = { args: { entityName: "Categories", gridHeight: "16rem" } };

export const SingleSelection: Story = { name: "Single selection, no borders", args: { selectionMode: "single", columnBorders: false, withBorder: false } };

export const SlowLoading: Story = { name: "Slow loading", args: { latency: 3000 } };
