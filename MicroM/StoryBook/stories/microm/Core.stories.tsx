import { Button, Code, Group, SimpleGrid, Stack, Text } from "@mantine/core";
import {
    AlertError, AlertInfo, AlertSuccess, CircleFilledIcon, CodeBlock, ColorConfiguration, ColorConfigurationValue, ConfirmAndExecutePanel,
    CopyToClipboard, EntityColumn, FakeProgressBar, NotifyBitField, NotifyError, NotifyInfo, NotifySuccess, RingProgressField, SearchFilterInput,
    SearchInput, ToggleActionIcon, useModal, WeekPicker
} from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { IconBolt, IconDatabase, IconStar, IconStarFilled, IconUsers } from "@tabler/icons-react";
import { useMemo, useState } from "react";
import { CoreShowcase } from "../showcase/MicroMShowcase";

const meta = {
    title: "MicroM/Core/Misc",
    parameters: { controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const Overview: Story = { render: () => <CoreShowcase /> };

export const Toggle: Story = {
    name: "ToggleActionIcon",
    render: () => (
        <Group>
            {(["filled", "light", "outline", "subtle", "default"] as const).map(v => (
                <ToggleActionIcon key={v} size="lg" hidden={false} onColor="yellow" offColor="gray" onVariant={v} offVariant={v}
                    onIcon={<IconStarFilled size="1.1rem" />} offIcon={<IconStar size="1.1rem" />} title={v} />
            ))}
        </Group>
    ),
};

function SearchStory() {
    const [value, setValue] = useState("");
    const [last, setLast] = useState("");
    return (
        <Stack maw="30rem">
            <SearchInput value={value} onChange={e => setValue(e.currentTarget.value)} onSearchClick={() => setLast(value)} size="md" iconsSize="1.1rem" placeholder="Search..." />
            <Text size="sm" color="dimmed">Last search: {last || "(none)"}</Text>
        </Stack>
    );
}

export const Search: Story = { name: "SearchInput", render: () => <SearchStory /> };

function WeekPickerStory() {
    const [start, setStart] = useState<Date | null>(new Date(2026, 8, 28));
    const [end, setEnd] = useState<Date | null>(null);
    return (
        <Stack>
            <WeekPicker weekStartDatevalue={start} setWeekStartValue={setStart} setWeekEndValue={setEnd} />
            <Text size="sm">Week: {start?.toLocaleDateString()} - {end?.toLocaleDateString()}</Text>
        </Stack>
    );
}

export const Week: Story = { name: "WeekPicker", render: () => <WeekPickerStory /> };

export const CodeAndCopy: Story = {
    name: "CodeBlock and CopyToClipboard",
    render: () => (
        <Stack>
            <CodeBlock language="tsx" codeText={`<MantineProvider theme={{ ...myTheme, colorScheme }} withGlobalStyles withNormalizeCSS>\n    <App />\n</MantineProvider>`} />
            <Group><Text size="sm">Copy a value:</Text><CopyToClipboard valueToCopy="copied value" /></Group>
        </Stack>
    ),
};

function ConfirmStory() {
    const modal = useModal();
    return (
        <Button color="red" variant="light" onClick={() => modal.open({
            modalProps: { title: <Text fw={700}>Confirm operation</Text> },
            content: <ConfirmAndExecutePanel
                content={<Text size="sm">Run the process? (simulated, takes 1.5 s)</Text>}
                operation="proc"
                okButtonText="Run"
                cancelButtonText="Cancel"
                onCancel={async () => { await modal.close(); }}
                onOK={async () => {
                    await new Promise(r => setTimeout(r, 1500));
                    await modal.close();
                    return { Failed: false, AutonumReturned: false, Results: [] };
                }}
            />
        })}>Open ConfirmAndExecutePanel</Button>
    );
}

export const Confirm: Story = { name: "ConfirmAndExecutePanel (modal)", render: () => <ConfirmStory /> };

export const Notifications: Story = {
    name: "Alerts and notifications",
    render: () => {
        const on = new EntityColumn<boolean>({ name: "bt_on", type: "bit", flags: 0, prompt: "On" });
        on.value = true;
        const off = new EntityColumn<boolean>({ name: "bt_off", type: "bit", flags: 0, prompt: "Off" });
        off.value = false;
        return (
            <SimpleGrid cols={2} breakpoints={[{ maxWidth: "sm", cols: 1 }]}>
                <AlertInfo title="AlertInfo">Informational message.</AlertInfo>
                <AlertSuccess title="AlertSuccess">Operation succeeded.</AlertSuccess>
                <AlertError title="AlertError">Something went wrong.</AlertError>
                <NotifyInfo title="NotifyInfo">There are updates.</NotifyInfo>
                <NotifySuccess title="NotifySuccess">Record saved.</NotifySuccess>
                <NotifyError title="NotifyError">The record could not be saved.</NotifyError>
                <NotifyBitField column={on} title="NotifyBitField (true)" trueMessage="Enabled" falseMessage="Disabled" />
                <NotifyBitField column={off} title="NotifyBitField (false)" trueMessage="Enabled" falseMessage="Disabled" />
            </SimpleGrid>
        );
    },
};

function ColorStory() {
    const [value, setValue] = useState<ColorConfigurationValue>({ colorKey: "blue", colorShade: 6 });
    return (
        <Stack maw="30rem">
            <ColorConfiguration value={value} onChange={setValue} />
            <Text size="sm">Value: <Code>{JSON.stringify(value)}</Code></Text>
        </Stack>
    );
}

export const Color: Story = { name: "ColorConfiguration", render: () => <ColorStory /> };

function SearchFilterStory() {
    const [values, setValues] = useState<string[]>(["customer"]);
    const [last, setLast] = useState("");
    return (
        <Stack maw="40rem">
            <SearchFilterInput data={values} value={values} onChange={setValues} onSearchClick={() => setLast(values.join(", "))} size="md" iconsSize="1.1rem"
                placeholder="Add search terms" />
            <Text size="sm" color="dimmed">Last search: {last || "(none)"}</Text>
        </Stack>
    );
}

export const SearchFilter: Story = { name: "SearchFilterInput", render: () => <SearchFilterStory /> };

export const Icons: Story = {
    name: "CircleFilledIcon",
    render: () => (
        <Group>
            <CircleFilledIcon icon={<IconUsers size="1rem" />} backColor="blue" color="white" width="2rem" />
            <CircleFilledIcon icon={<IconDatabase size="1rem" />} backColor="teal" color="white" width="2.5rem" />
            <CircleFilledIcon icon={<IconBolt size="1.2rem" />} backColor="orange" color="white" width="3rem" />
        </Group>
    ),
};

function StatsStory() {
    const columns = useMemo(() => [3, 7, 10].map((v, i) => {
        const c = new EntityColumn<number>({ name: `i_value${i}`, type: "int", flags: 0, prompt: "Value" });
        c.value = v;
        return c;
    }), []);
    return (
        <Stack>
            <SimpleGrid cols={3} breakpoints={[{ maxWidth: "md", cols: 1 }]}>
                <RingProgressField column={columns[0]} maxValue={10} title="Percent" description="3 of 10" displayPercent="percent" withBorder />
                <RingProgressField column={columns[1]} maxValue={10} title="Fraction" description="7 of 10" displayPercent="fraction" color="teal" withBorder showPercentAsCenterLabel />
                <RingProgressField column={columns[2]} maxValue={10} title="Completed" description="10 of 10" color="green" withBorder centerIcon={IconStarFilled} />
            </SimpleGrid>
            <Text size="sm">FakeProgressBar</Text>
            <FakeProgressBar />
        </Stack>
    );
}

export const Stats: Story = { name: "RingProgressField and FakeProgressBar", render: () => <StatsStory /> };
