import { Group, SimpleGrid, Stack, Text } from "@mantine/core";
import {
    AlertError, AlertInfo, AlertSuccess, CircleFilledIcon, CopyToClipboard, DataGrid, EntityColumn, FakeProgressBar, NotifyBitField, NotifyInfo,
    NotifySuccess, RingProgressField, SearchInput, ToggleActionIcon
} from "@mcurros2/microm";
import { IconBell, IconBellOff, IconDatabase, IconStar, IconStarFilled, IconUsers } from "@tabler/icons-react";
import { useMemo, useState } from "react";
import { CustomersForm } from "../mocks/CustomersForm";
import { Customers, FieldsDemo } from "../mocks/entities";
import { FieldsDemoForm } from "../mocks/FieldsDemoForm";
import { createMockClient } from "../mocks/mockClient";
import { ShowcaseSection } from "./MantineShowcase";

export function useMockClient(latency = 350) {
    return useMemo(() => createMockClient({ latency }), [latency]);
}

export function DataGridShowcase({ height = "22rem" }: { height?: string }) {
    const client = useMockClient();
    const entity = useMemo(() => new Customers(client), [client]);
    return (
        <ShowcaseSection title="DataGrid" description="MicroM grid against the mock backend: search, add, edit, delete, export.">
            <DataGrid entity={entity} viewName={entity.def.views.cus_brwStandard.name} selectionMode="multi" gridHeight={height} />
        </ShowcaseSection>
    );
}

export function CustomersFormShowcase() {
    const client = useMockClient();
    const entity = useMemo(() => {
        const e = new Customers(client);
        e.def.columns.c_customer_id.value = "1";
        return e;
    }, [client]);
    return (
        <ShowcaseSection title="EntityForm" description="Custom form (edit mode) with lookup, date and number fields.">
            <CustomersForm entity={entity} initialFormMode="edit" />
        </ShowcaseSection>
    );
}

export function FieldsShowcase() {
    const client = useMockClient();
    const entity = useMemo(() => {
        const e = new FieldsDemo(client);
        e.def.columns.c_demo_id.value = "DEMO1";
        return e;
    }, [client]);
    return (
        <ShowcaseSection title="MicroM fields" description="Every *Field inside an EntityForm (edit mode).">
            <FieldsDemoForm entity={entity} initialFormMode="edit" />
        </ShowcaseSection>
    );
}

export function CoreShowcase() {
    const [search, setSearch] = useState("");
    const ring = useMemo(() => {
        const c = new EntityColumn<number>({ name: "i_progress", type: "int", flags: 0, prompt: "Progress" });
        c.value = 7;
        return c;
    }, []);
    const bit = useMemo(() => {
        const c = new EntityColumn<boolean>({ name: "bt_ok", type: "bit", flags: 0, prompt: "OK" });
        c.value = true;
        return c;
    }, []);

    return (
        <ShowcaseSection title="Core components" description="MicroM alerts, notifications, icons and utilities.">
            <SimpleGrid cols={3} breakpoints={[{ maxWidth: "md", cols: 1 }]}>
                <AlertInfo title="AlertInfo">Informational message.</AlertInfo>
                <AlertSuccess title="AlertSuccess">Operation succeeded.</AlertSuccess>
                <AlertError title="AlertError">Something went wrong.</AlertError>
                <NotifySuccess title="NotifySuccess">Record saved.</NotifySuccess>
                <NotifyInfo title="NotifyInfo">There are updates.</NotifyInfo>
                <NotifyBitField column={bit} title="NotifyBitField" trueMessage="The value is true" falseMessage="The value is false" />
            </SimpleGrid>
            <Group>
                <SearchInput value={search} onChange={e => setSearch(e.currentTarget.value)} onSearchClick={() => { }} size="sm" iconsSize="1rem" placeholder="SearchInput" w="18rem" />
                <ToggleActionIcon size="lg" onColor="yellow" offColor="gray" onIcon={<IconStarFilled size="1.1rem" />} offIcon={<IconStar size="1.1rem" />} hidden={false} initialStatus="on" title="ToggleActionIcon" />
                <ToggleActionIcon size="lg" onColor="blue" offColor="gray" onIcon={<IconBell size="1.1rem" />} offIcon={<IconBellOff size="1.1rem" />} hidden={false} onVariant="filled" />
                <CopyToClipboard valueToCopy="Copied from Storybook" />
                <CircleFilledIcon icon={<IconUsers size="1rem" />} backColor="blue" color="white" width="2rem" />
                <CircleFilledIcon icon={<IconDatabase size="1rem" />} backColor="teal" color="white" width="2rem" />
            </Group>
            <Group grow align="flex-start">
                <RingProgressField column={ring} maxValue={10} title="RingProgressField" description="7 of 10 tasks" withBorder />
                <Stack spacing={4}>
                    <Text size="sm">FakeProgressBar</Text>
                    <FakeProgressBar />
                </Stack>
            </Group>
        </ShowcaseSection>
    );
}

export function MicroMShowcase() {
    return (
        <Stack>
            <CoreShowcase />
            <DataGridShowcase />
            <CustomersFormShowcase />
            <FieldsShowcase />
        </Stack>
    );
}
