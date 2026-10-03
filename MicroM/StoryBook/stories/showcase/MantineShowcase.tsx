import {
    Accordion, ActionIcon, Alert, Anchor, Avatar, Badge, Blockquote, Breadcrumbs, Button, Card, Checkbox, Chip, Code, ColorSwatch, Group,
    Kbd, Loader, MantineColor, Menu, MultiSelect, Notification, NumberInput, Pagination, Paper, PasswordInput, Progress, Radio,
    SegmentedControl, Select, SimpleGrid, Skeleton, Slider, Stack, Stepper, Switch, Table, Tabs, Text, Textarea, TextInput,
    ThemeIcon, Timeline, Title, Tooltip, useMantineTheme
} from "@mantine/core";
import { DatePickerInput } from "@mantine/dates";
import {
    IconAdjustments, IconAlertCircle, IconBell, IconCheck, IconChevronDown, IconDots, IconEdit, IconHeart, IconHome, IconMail,
    IconPlus, IconSearch, IconSettings, IconTrash, IconUser
} from "@tabler/icons-react";
import { ReactNode, useState } from "react";
import { readableTextColor } from "../theme-editor/colorUtils";

const VARIANTS = ["filled", "light", "outline", "subtle", "default", "white", "gradient"] as const;

export function ShowcaseSection({ title, description, children }: { title: string, description?: string, children: ReactNode }) {
    return (
        <Paper withBorder p="md">
            <Stack spacing="sm">
                <div>
                    <Title order={5}>{title}</Title>
                    {description && <Text size="xs" color="dimmed">{description}</Text>}
                </div>
                {children}
            </Stack>
        </Paper>
    );
}

export function ButtonsShowcase({ color }: { color?: MantineColor }) {
    const theme = useMantineTheme();
    // Mantine 6 ActionIcon defaults to "gray"
    const iconColor = color ?? theme.primaryColor;
    return (
        <ShowcaseSection title="Buttons" description="Variants, sizes and states. Driven by primaryColor, radius and defaultGradient.">
            <Group>
                {VARIANTS.map(v => <Button key={v} variant={v} color={color}>{v}</Button>)}
            </Group>
            <Group>
                {(["xs", "sm", "md", "lg", "xl"] as const).map(s => <Button key={s} size={s} color={color}>Size {s}</Button>)}
            </Group>
            <Group>
                <Button leftIcon={<IconPlus size="1rem" />} color={color}>Add</Button>
                <Button variant="light" rightIcon={<IconChevronDown size="1rem" />} color={color}>Actions</Button>
                <Button loading color={color}>Saving</Button>
                <Button disabled>Disabled</Button>
                <Button compact variant="outline" color={color}>Compact</Button>
                <Button color="red" variant="light" leftIcon={<IconTrash size="1rem" />}>Delete</Button>
            </Group>
            <Group>
                {(["filled", "light", "outline", "subtle", "default", "transparent"] as const).map(v => (
                    <Tooltip key={v} label={`ActionIcon ${v}`}>
                        <ActionIcon variant={v} color={iconColor}><IconSettings size="1.1rem" /></ActionIcon>
                    </Tooltip>
                ))}
                <ActionIcon variant="filled" radius="xl" color={iconColor}><IconHeart size="1.1rem" /></ActionIcon>
            </Group>
        </ShowcaseSection>
    );
}

export function InputsShowcase() {
    const [date, setDate] = useState<Date | null>(new Date(2026, 8, 30));
    return (
        <ShowcaseSection title="Inputs" description="@mantine/core and @mantine/dates inputs">
            <SimpleGrid cols={2} breakpoints={[{ maxWidth: "sm", cols: 1 }]}>
                <TextInput label="Text" placeholder="Type something" description="Field description" withAsterisk />
                <TextInput label="With icon" placeholder="Search..." icon={<IconSearch size="1rem" />} />
                <TextInput label="With error" defaultValue="invalid value" error="This field is invalid" />
                <PasswordInput label="Password" defaultValue="secret" />
                <Select label="Select" placeholder="Pick one" data={["Option A", "Option B", "Option C"]} defaultValue="Option A" />
                <MultiSelect label="MultiSelect" data={["React", "Mantine", "MicroM", "SQL Server"]} defaultValue={["MicroM", "Mantine"]} />
                <NumberInput label="Number" defaultValue={42} />
                <DatePickerInput label="Date" value={date} onChange={setDate} />
                <TextInput label="Disabled" disabled defaultValue="Read only" />
                <TextInput label="Variant filled" variant="filled" placeholder="filled" />
            </SimpleGrid>
            <Textarea label="Textarea" minRows={2} defaultValue="Multiline text" />
            <Group spacing="xl" align="flex-start">
                <Stack spacing="xs">
                    <Checkbox label="Checkbox" defaultChecked />
                    <Checkbox label="Indeterminate" indeterminate />
                    <Checkbox label="Disabled" disabled />
                </Stack>
                <Stack spacing="xs">
                    <Switch label="Switch" defaultChecked />
                    <Switch label="Off" />
                </Stack>
                <Radio.Group defaultValue="b" label="Radio">
                    <Group mt="xs"><Radio value="a" label="A" /><Radio value="b" label="B" /><Radio value="c" label="C" /></Group>
                </Radio.Group>
            </Group>
            <SegmentedControl data={["Day", "Week", "Month", "Year"]} defaultValue="Week" />
            <Slider defaultValue={40} marks={[{ value: 20, label: "20%" }, { value: 50, label: "50%" }, { value: 80, label: "80%" }]} mb="lg" />
            <Chip.Group defaultValue={["a"]} multiple>
                <Group spacing="xs"><Chip value="a">Active chip</Chip><Chip value="b">Chip</Chip><Chip value="c" variant="filled">Filled</Chip></Group>
            </Chip.Group>
        </ShowcaseSection>
    );
}

export function FeedbackShowcase() {
    return (
        <ShowcaseSection title="Feedback" description="Alerts, notifications, progress and badges">
            <SimpleGrid cols={2} breakpoints={[{ maxWidth: "sm", cols: 1 }]}>
                <Alert icon={<IconAlertCircle size="1rem" />} title="Alert light">Message using the primary color.</Alert>
                <Alert icon={<IconAlertCircle size="1rem" />} title="Alert filled" variant="filled" color="red">Something went wrong.</Alert>
                <Notification title="Notification" icon={<IconCheck size="1rem" />} color="teal" withCloseButton={false}>Saved successfully</Notification>
                <Notification title="Loading" loading withCloseButton={false}>Processing the request...</Notification>
            </SimpleGrid>
            <Progress value={65} label="65%" size="xl" />
            <Progress sections={[{ value: 35, color: "blue" }, { value: 25, color: "teal" }, { value: 15, color: "orange" }]} />
            <Group>
                <Loader /><Loader variant="bars" /><Loader variant="dots" />
                {(["filled", "light", "outline", "dot", "gradient"] as const).map(v => <Badge key={v} variant={v}>{v}</Badge>)}
                <Badge color="red">Error</Badge><Badge color="green">Active</Badge>
            </Group>
            <Group>
                <ThemeIcon><IconUser size="1rem" /></ThemeIcon>
                <ThemeIcon variant="light"><IconMail size="1rem" /></ThemeIcon>
                <ThemeIcon variant="outline"><IconBell size="1rem" /></ThemeIcon>
                <ThemeIcon variant="gradient" radius="xl"><IconHeart size="1rem" /></ThemeIcon>
                <Avatar radius="xl">MC</Avatar>
                <Avatar radius="xl" color="blue" variant="filled">AB</Avatar>
                <Skeleton height={24} width={120} radius="sm" />
            </Group>
        </ShowcaseSection>
    );
}

export function NavigationShowcase() {
    const [active, setActive] = useState(1);
    return (
        <ShowcaseSection title="Navigation and containers">
            <Breadcrumbs>
                <Anchor href="#" onClick={e => e.preventDefault()}>Home</Anchor>
                <Anchor href="#" onClick={e => e.preventDefault()}>Customers</Anchor>
                <Text size="sm">Detail</Text>
            </Breadcrumbs>
            <Tabs defaultValue="general">
                <Tabs.List>
                    <Tabs.Tab value="general" icon={<IconHome size="0.9rem" />}>General</Tabs.Tab>
                    <Tabs.Tab value="config" icon={<IconSettings size="0.9rem" />}>Settings</Tabs.Tab>
                    <Tabs.Tab value="other">Other</Tabs.Tab>
                </Tabs.List>
                <Tabs.Panel value="general" pt="xs"><Text size="sm">General tab content.</Text></Tabs.Panel>
                <Tabs.Panel value="config" pt="xs"><Text size="sm">Settings.</Text></Tabs.Panel>
                <Tabs.Panel value="other" pt="xs"><Text size="sm">Other.</Text></Tabs.Panel>
            </Tabs>
            <Tabs defaultValue="a" variant="pills">
                <Tabs.List><Tabs.Tab value="a">Pills</Tabs.Tab><Tabs.Tab value="b">Another</Tabs.Tab></Tabs.List>
            </Tabs>
            <Stepper active={active} onStepClick={setActive} size="sm">
                <Stepper.Step label="Data" description="Step 1" />
                <Stepper.Step label="Review" description="Step 2" />
                <Stepper.Step label="Confirm" description="Step 3" />
            </Stepper>
            <Group position="apart">
                <Pagination total={10} defaultValue={3} />
                <Menu shadow="md" width={200}>
                    <Menu.Target><Button variant="default" rightIcon={<IconDots size="1rem" />}>Menu</Button></Menu.Target>
                    <Menu.Dropdown>
                        <Menu.Label>Record</Menu.Label>
                        <Menu.Item icon={<IconEdit size="0.9rem" />}>Edit</Menu.Item>
                        <Menu.Item icon={<IconAdjustments size="0.9rem" />}>Options</Menu.Item>
                        <Menu.Divider />
                        <Menu.Item color="red" icon={<IconTrash size="0.9rem" />}>Delete</Menu.Item>
                    </Menu.Dropdown>
                </Menu>
            </Group>
            <Accordion variant="separated" defaultValue="one">
                <Accordion.Item value="one"><Accordion.Control>Separated accordion</Accordion.Control><Accordion.Panel>Panel content.</Accordion.Panel></Accordion.Item>
                <Accordion.Item value="two"><Accordion.Control>Another item</Accordion.Control><Accordion.Panel>More content.</Accordion.Panel></Accordion.Item>
            </Accordion>
            <SimpleGrid cols={3} breakpoints={[{ maxWidth: "sm", cols: 1 }]}>
                {(["xs", "md", "xl"] as const).map(s => (
                    <Card key={s} shadow={s} withBorder>
                        <Text fw={600}>Card shadow {s}</Text>
                        <Text size="sm" color="dimmed">Sample content inside a Card.</Text>
                        <Button variant="light" fullWidth mt="md">Action</Button>
                    </Card>
                ))}
            </SimpleGrid>
            <Timeline active={1} bulletSize={20} lineWidth={2}>
                <Timeline.Item title="Created"><Text size="xs" color="dimmed">Record created</Text></Timeline.Item>
                <Timeline.Item title="Updated"><Text size="xs" color="dimmed">Data was updated</Text></Timeline.Item>
                <Timeline.Item title="Pending"><Text size="xs" color="dimmed">Approval</Text></Timeline.Item>
            </Timeline>
        </ShowcaseSection>
    );
}

export function TypographyShowcase() {
    return (
        <ShowcaseSection title="Typography" description="Theme fontFamily, headings, fontSizes and lineHeight">
            <Title order={1}>Heading h1</Title>
            <Title order={2}>Heading h2</Title>
            <Title order={3}>Heading h3</Title>
            <Title order={4}>Heading h4</Title>
            <Title order={5}>Heading h5</Title>
            <Title order={6}>Heading h6</Title>
            {(["xs", "sm", "md", "lg", "xl"] as const).map(s => <Text key={s} size={s}>Text size {s}: The quick brown fox jumps over the lazy dog.</Text>)}
            <Text color="dimmed" size="sm">Dimmed text</Text>
            <Group><Code>const x = 1;</Code><Kbd>Ctrl</Kbd> + <Kbd>K</Kbd><Anchor href="#" onClick={e => e.preventDefault()}>A link</Anchor></Group>
            <Blockquote cite="- Sample author">A blockquote to check colors and typography.</Blockquote>
        </ShowcaseSection>
    );
}

export function TableShowcase() {
    const rows = [
        ["1", "Alice Demo", "Wholesale", "$ 120,000"],
        ["2", "Bruce Example", "Retail", "$ 45,500"],
        ["3", "Carol Test", "Corporate", "$ 310,000"],
    ];
    return (
        <ShowcaseSection title="Table (Mantine)">
            <Table striped highlightOnHover withBorder withColumnBorders>
                <thead><tr><th>#</th><th>Name</th><th>Category</th><th>Amount</th></tr></thead>
                <tbody>{rows.map(r => <tr key={r[0]}>{r.map((c, i) => <td key={i}>{c}</td>)}</tr>)}</tbody>
            </Table>
        </ShowcaseSection>
    );
}

export function PaletteShowcase() {
    const theme = useMantineTheme();
    const primaryShade = typeof theme.primaryShade === "number" ? theme.primaryShade : theme.primaryShade[theme.colorScheme];
    return (
        <ShowcaseSection title="Theme palette" description={`primaryColor: ${theme.primaryColor} - primaryShade (${theme.colorScheme}): ${primaryShade}`}>
            <Stack spacing={4}>
                {Object.entries(theme.colors).map(([name, shades]) => (
                    <Group key={name} spacing={4} noWrap>
                        <Text size="xs" w={70} fw={name === theme.primaryColor ? 700 : 400}>{name}</Text>
                        {shades.map((c, i) => (
                            <Tooltip key={i} label={`${name}.${i} ${c}`} withinPortal>
                                <ColorSwatch color={c} radius="sm" size={28}
                                    sx={{ outline: name === theme.primaryColor && i === primaryShade ? `2px solid ${theme.colorScheme === "dark" ? "#fff" : "#000"}` : undefined }}>
                                    <Text size={9} color={readableTextColor(c)}>{i}</Text>
                                </ColorSwatch>
                            </Tooltip>
                        ))}
                    </Group>
                ))}
            </Stack>
        </ShowcaseSection>
    );
}

export function MantineShowcase() {
    return (
        <Stack>
            <ButtonsShowcase />
            <InputsShowcase />
            <FeedbackShowcase />
            <NavigationShowcase />
            <TableShowcase />
            <TypographyShowcase />
        </Stack>
    );
}
