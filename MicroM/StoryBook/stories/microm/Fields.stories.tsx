import type { Meta, StoryObj } from "@storybook/react-vite";
import { useMemo } from "react";
import { FieldsDemo } from "../mocks/entities";
import { FieldsDemoForm } from "../mocks/FieldsDemoForm";
import { createMockClient } from "../mocks/mockClient";

interface FieldsArgs {
    formMode: "add" | "edit" | "view",
    showHelpButton: boolean,
    okButtonVariant: "filled" | "light" | "outline" | "default" | "subtle",
    cancelButtonVariant: "filled" | "light" | "outline" | "default" | "subtle",
}

function FieldsDemoStory({ formMode, ...rest }: FieldsArgs) {
    const entity = useMemo(() => {
        const e = new FieldsDemo(createMockClient());
        if (formMode !== "add") e.def.columns.c_demo_id.value = "DEMO1";
        return e;
    }, [formMode]);
    return <FieldsDemoForm key={formMode} entity={entity} initialFormMode={formMode} {...rest} />;
}

const meta = {
    title: "MicroM/Forms/Fields",
    component: FieldsDemoStory,
    args: {
        formMode: "edit",
        showHelpButton: true,
        okButtonVariant: "filled",
        cancelButtonVariant: "light",
    },
    argTypes: {
        formMode: { control: "inline-radio", options: ["add", "edit", "view"] },
        okButtonVariant: { control: "select", options: ["filled", "light", "outline", "default", "subtle"] },
        cancelButtonVariant: { control: "select", options: ["filled", "light", "outline", "default", "subtle"] },
    },
} satisfies Meta<typeof FieldsDemoStory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Edit: Story = {};
export const Add: Story = { args: { formMode: "add" } };
export const ReadOnly: Story = { name: "Read only", args: { formMode: "view" } };
