import { Button, Code, Group, Stack, Text } from "@mantine/core";
import {
    DataGridSelectionKeys, EntityForm, FormMode, LookupFormAction as MicroMLookupFormAction, useEntityForm, useLookupForm, useModal, ValuesObject
} from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { useMemo, useState } from "react";
import { Categories, Customers, FieldsDemo, Tags } from "../mocks/entities";
import { LookupsSection, LookupsSectionProps } from "../mocks/LookupsSection";
import { createMockClient } from "../mocks/mockClient";

interface LookupStoryArgs {
    formMode: FormMode,
    show: LookupsSectionProps["show"],
}

function LookupStory({ formMode, show }: LookupStoryArgs) {
    const entity = useMemo(() => {
        const e = new FieldsDemo(createMockClient());
        if (formMode !== "add") e.def.columns.c_demo_id.value = "DEMO1";
        return e;
    }, [formMode]);
    const entityForm = useEntityForm({ entity, initialFormMode: formMode, getDataOnInit: formMode !== "add" });
    const v = entityForm.form.values;
    return (
        <EntityForm formAPI={entityForm} key={formMode}>
            <Stack>
                <LookupsSection entity={entity} entityForm={entityForm} show={show} />
                <Text size="xs" color="dimmed">
                    Form values: <Code>{JSON.stringify({ c_category_id: v.c_category_id, c_category2_id: v.c_category2_id, vc_tags: v.vc_tags, c_country_id: v.c_country_id, c_province_id: v.c_province_id })}</Code>
                </Text>
            </Stack>
        </EntityForm>
    );
}

const meta = {
    title: "MicroM/Lookups",
    component: LookupStory,
    args: { formMode: "edit", show: ["lookup", "select", "multiselect", "compound"] },
    argTypes: {
        formMode: { control: "inline-radio", options: ["add", "edit", "view"] },
        show: { control: "check", options: ["lookup", "select", "multiselect", "compound", "compoundLastLevel"] },
    },
} satisfies Meta<typeof LookupStory>;

export default meta;
type Story = StoryObj<typeof meta>;

export const All: Story = {};
export const Lookup: Story = { args: { show: ["lookup"] } };
export const LookupSelect: Story = { args: { show: ["select"] } };
export const LookupMultiSelect: Story = { args: { show: ["multiselect"] } };
export const CompoundLookup: Story = { args: { show: ["compound"] } };
export const CompoundLookupLastLevel: Story = { name: "CompoundLookup (editLastLevelOnly)", args: { show: ["compoundLastLevel"] } };
export const AddMode: Story = { name: "All (add mode)", args: { formMode: "add" } };

function LookupFormStory() {
    const client = useMemo(() => createMockClient(), []);
    const openLookup = useLookupForm();
    const [selected, setSelected] = useState<ValuesObject[]>([]);
    const open = (selectionMode: "single" | "multi") => {
        const entity = new Customers(client);
        openLookup({
            entity, viewName: entity.def.views.cus_brwStandard.name, selectionMode, onOK: setSelected,
            modalProps: { title: <Text fw={700}>Select customers</Text>, size: "xl" },
        });
    };
    return (
        <Stack>
            <Group>
                <Button onClick={() => open("single")}>useLookupForm (single)</Button>
                <Button variant="light" onClick={() => open("multi")}>useLookupForm (multi)</Button>
            </Group>
            <Text size="sm">Selected keys: <Code>{JSON.stringify(selected)}</Code></Text>
        </Stack>
    );
}

export const LookupForm: Story = { name: "LookupForm (useLookupForm)", render: () => <LookupFormStory />, parameters: { controls: { disable: true } } };

function LookupFormActionStory() {
    const client = useMemo(() => createMockClient(), []);
    const modal = useModal();
    const [result, setResult] = useState("");
    const open = () => {
        const tags = new Tags(client);
        modal.open({
            modalProps: { title: <Text fw={700}>Add tags to the selected customers</Text>, size: "lg" },
            content: <MicroMLookupFormAction lookupEntity={tags} viewName={tags.def.views.tag_brwStandard.name} title="Pick the tags to add"
                onOK={async (keys: DataGridSelectionKeys) => {
                    await new Promise(r => setTimeout(r, 800));
                    setResult(JSON.stringify(keys));
                    return { Failed: false, AutonumReturned: false, Results: [{ Status: 0, Message: "OK" }] };
                }} />,
        });
    };
    return (
        <Stack>
            <Group><Button onClick={open}>Open LookupFormAction</Button></Group>
            <Text size="sm">Last result: <Code>{result || "(none)"}</Code></Text>
            <Text size="xs" color="dimmed">Typical use: from an EntityClientAction to bulk-assign related records.</Text>
        </Stack>
    );
}

export const LookupFormAction: Story = { name: "LookupFormAction", render: () => <LookupFormActionStory />, parameters: { controls: { disable: true } } };

function CategoriesStandalone() {
    const client = useMemo(() => createMockClient(), []);
    const openLookup = useLookupForm();
    return <Button variant="default" onClick={() => {
        const c = new Categories(client);
        openLookup({ entity: c, viewName: "cat_brwStandard", onOK: () => { }, enableAdd: true, enableEdit: true, enableDelete: true, selectionMode: "single" });
    }}>Lookup grid with add/edit/delete enabled</Button>;
}

export const LookupFormEditable: Story = { name: "LookupForm (editable grid)", render: () => <CategoriesStandalone />, parameters: { controls: { disable: true } } };
