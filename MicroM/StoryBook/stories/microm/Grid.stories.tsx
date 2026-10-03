import { Text } from "@mantine/core";
import { Grid, GridColumn, GridColumnsOverrides, GridSourceRecord } from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { IconCheck, IconX } from "@tabler/icons-react";
import { useMemo } from "react";
import { categoryDescription, customersRows } from "../mocks/mockClient";

interface GridArgs {
    selectionMode: "single" | "multi",
    showSelectCheckbox: boolean,
    stripped: boolean,
    highlightOnHover: boolean,
    columnBorders: boolean,
    rowBorders: boolean,
    withBorder: boolean,
    autoSizeColumnsOnLoad: boolean,
    rows: number,
}

const columns: GridColumn[] = [
    { text: "Id", field: "0", sqlType: "char" },
    { text: "Full name", field: "1", sqlType: "varchar" },
    { text: "Email", field: "2", sqlType: "varchar" },
    { text: "Category", field: "3", sqlType: "varchar" },
    { text: "Since", field: "4", sqlType: "date" },
    { text: "Credit limit", field: "5", sqlType: "money" },
    { text: "Active", field: "6", sqlType: "bit" },
];

function GridStory({ rows, ...props }: GridArgs) {
    const data = useMemo<GridSourceRecord[]>(() => {
        const out: GridSourceRecord[] = [];
        for (let i = 0; i < rows; i++) {
            const r = customersRows[i % customersRows.length];
            out.push([String(i + 1), r.vc_name, r.vc_email, categoryDescription(r.c_category_id), r.dt_since, r.m_credit_limit, r.bt_active] as GridSourceRecord);
        }
        return out;
    }, [rows]);

    const overrides = useMemo<GridColumnsOverrides>(() => ({
        "6": { render: v => v ? <IconCheck size="1rem" color="green" /> : <IconX size="1rem" color="gray" /> },
    }), []);

    return (
        <>
            <Grid rows={data} columns={columns} columnsOverrides={overrides} gridHeight="28rem" {...props} />
            <Text size="xs" color="dimmed" mt="xs">{data.length} rows</Text>
        </>
    );
}

const meta = {
    title: "MicroM/Data/Grid",
    component: GridStory,
    args: {
        selectionMode: "multi",
        showSelectCheckbox: false,
        stripped: false,
        highlightOnHover: true,
        columnBorders: true,
        rowBorders: false,
        withBorder: true,
        autoSizeColumnsOnLoad: true,
        rows: 500,
    },
    argTypes: {
        selectionMode: { control: "inline-radio", options: ["single", "multi"] },
        rows: { control: { type: "range", min: 0, max: 20000, step: 100 } },
    },
} satisfies Meta<typeof GridStory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Basic: Story = {};
export const Striped: Story = { args: { stripped: true, showSelectCheckbox: true } };
export const Empty: Story = { args: { rows: 0 } };
