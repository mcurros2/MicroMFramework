import { Button, Group, Stack, Text } from "@mantine/core";
import { AutoForm, FormMode, useOpenForm } from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { useMemo } from "react";
import { CustomersForm } from "../mocks/CustomersForm";
import { Customers, CustomersAutoForm } from "../mocks/entities";
import { createMockClient } from "../mocks/mockClient";

interface FormArgs {
    formMode: FormMode,
    customerId: string,
    OKText: string,
    CancelText: string,
    okButtonVariant: "filled" | "light" | "outline" | "default" | "subtle" | "gradient",
    cancelButtonVariant: "filled" | "light" | "outline" | "default" | "subtle",
    showHelpButton: boolean,
}

function CustomersFormStory({ formMode, customerId, ...rest }: FormArgs) {
    const entity = useMemo(() => {
        const e = new Customers(createMockClient());
        if (formMode !== "add") e.def.columns.c_customer_id.value = customerId;
        return e;
    }, [formMode, customerId]);
    return <CustomersForm key={`${formMode}${customerId}`} entity={entity} initialFormMode={formMode} {...rest} />;
}

const meta = {
    title: "MicroM/Forms/EntityForm",
    component: CustomersFormStory,
    args: {
        formMode: "edit",
        customerId: "1",
        OKText: "Save",
        CancelText: "Cancel",
        okButtonVariant: "filled",
        cancelButtonVariant: "light",
        showHelpButton: false,
    },
    argTypes: {
        formMode: { control: "inline-radio", options: ["add", "edit", "view"] },
        okButtonVariant: { control: "select", options: ["filled", "light", "outline", "default", "subtle", "gradient"] },
        cancelButtonVariant: { control: "select", options: ["filled", "light", "outline", "default", "subtle"] },
    },
} satisfies Meta<typeof CustomersFormStory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Edit: Story = {};

export const Add: Story = { args: { formMode: "add" } };

export const ReadOnly: Story = { name: "Read only", args: { formMode: "view" } };

export const WithAutoForm: Story = {
    name: "AutoForm",
    render: ({ formMode, customerId }) => {
        const entity = new CustomersAutoForm(createMockClient());
        if (formMode !== "add") entity.def.columns.c_customer_id.value = customerId;
        return <AutoForm key={`${formMode}${customerId}`} entity={entity} initialFormMode={formMode} getDataOnInit={formMode !== "add"} />;
    },
};

function ModalStory() {
    const entity = useMemo(() => new Customers(createMockClient()), []);
    const openForm = useOpenForm();
    return (
        <Stack>
            <Text size="sm">Opens the form inside the MicroM <code>ModalsManager</code>, the same way a DataGrid does.</Text>
            <Group>
                <Button onClick={() => openForm({ entity, initialFormMode: "add", title: "New customer", dontAddEntityTitle: true })}>New customer (modal)</Button>
                <Button variant="light" onClick={() => {
                    entity.def.columns.c_customer_id.value = "2";
                    openForm({ entity, initialFormMode: "edit", getDataOnInit: true, title: "Edit customer 2", dontAddEntityTitle: true });
                }}>Edit customer 2 (modal)</Button>
            </Group>
        </Stack>
    );
}

export const InModal: Story = {
    name: "In modal",
    render: () => <ModalStory />,
    parameters: { controls: { disable: true } },
};
