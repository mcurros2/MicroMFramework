import { Box, Button, Code, Group, Stack, Text } from "@mantine/core";
import {
    DateInputField, EmailField, EntityFormModal, Lookup, EntityFormPage, EntityFormPanel, ModalForm as MicroMModalForm, NumberField, StepperForm, TextField, useEntityForm
} from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { useMemo, useState } from "react";
import { Customers, CustomersAutoForm } from "../mocks/entities";
import { createMockClient } from "../mocks/mockClient";

const meta = {
    title: "MicroM/Forms/Form variants",
    parameters: { controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const Page: Story = {
    name: "EntityFormPage",
    render: () => <EntityFormPage client={createMockClient()} entityConstructor={c => { const e = new Customers(c); e.def.columns.c_customer_id.value = "3"; return e; }} initialFormMode="edit" />,
};

export const Panel: Story = {
    name: "EntityFormPanel",
    render: () => (
        <Box h="80vh">
            <EntityFormPanel client={createMockClient()} parentKeys={{ c_customer_id: "4" }} initialFormMode="edit" entityConstructor={(c, pk) => new Customers(c, pk)} />
        </Box>
    ),
};

function ModalStory() {
    const client = useMemo(() => createMockClient(), []);
    const [open, setOpen] = useState(false);
    const [mode, setMode] = useState<"add" | "edit">("add");
    return (
        <Stack>
            <Group>
                <Button onClick={() => { setMode("add"); setOpen(true); }}>New customer</Button>
                <Button variant="light" onClick={() => { setMode("edit"); setOpen(true); }}>Edit customer 5</Button>
            </Group>
            <EntityFormModal client={client} openState={open} setOpenState={setOpen} initialFormMode={mode} getDataOnInit={mode === "edit"}
                entityConstructor={c => { const e = new Customers(c); if (mode === "edit") e.def.columns.c_customer_id.value = "5"; return e; }} />
        </Stack>
    );
}

export const Modal: Story = { name: "EntityFormModal", render: () => <ModalStory /> };

function ModalFormStory() {
    const client = useMemo(() => createMockClient(), []);
    const [open, setOpen] = useState(false);
    const [last, setLast] = useState("");
    return (
        <Stack>
            <Group><Button onClick={() => setOpen(true)}>Open ModalForm (AutoForm)</Button></Group>
            {open && <MicroMModalForm entity={new CustomersAutoForm(client)}
                onSaved={() => { setLast("saved"); setOpen(false); }} onCancel={() => { setLast("cancelled"); setOpen(false); }} />}
            <Text size="sm">Last result: <Code>{last || "(none)"}</Code></Text>
        </Stack>
    );
}

export const ModalForm: Story = { name: "ModalForm", render: () => <ModalFormStory /> };

function StepperStory() {
    const entity = useMemo(() => new Customers(createMockClient()), []);
    const formAPI = useEntityForm({ entity, initialFormMode: "add", getDataOnInit: false, validateInputOnBlur: true });
    const cols = entity.def.columns;
    return (
        <StepperForm formAPI={formAPI} initialStep={0}
            completedContent={<Text>Customer created. Check the browser console for the mock insert.</Text>}
            steps={[
                {
                    name: "contact", label: "Contact", description: "Name and email", validateFields: ["vc_name"],
                    content: <Stack><TextField entityForm={formAPI} column={cols.vc_name} /><EmailField entityForm={formAPI} column={cols.vc_email} /></Stack>,
                },
                {
                    name: "commercial", label: "Commercial", description: "Since and credit", validateFields: ["dt_since"],
                    content: <Stack><DateInputField entityForm={formAPI} column={cols.dt_since} /><NumberField entityForm={formAPI} column={cols.m_credit_limit} precision={2} /></Stack>,
                },
                {
                    name: "location", label: "Location", description: "Country and province",
                    content: <StepperLookups entity={entity} formAPI={formAPI} />,
                },
            ]}
        />
    );
}

function StepperLookups({ entity, formAPI }: { entity: Customers, formAPI: ReturnType<typeof useEntityForm> }) {
    const binding = useMemo(() => [entity.def.columns.c_country_id, entity.def.columns.c_province_id], [entity]);
    return <Lookup entityForm={formAPI} entity={entity} bindingColumns={binding} lookupDefName={entity.def.lookups.Provinces.name} required={false} label="Country - Province" />;
}

export const Stepper: Story = { name: "StepperForm", render: () => <StepperStory /> };
