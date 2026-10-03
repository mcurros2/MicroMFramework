import { Box, Button, Code, Group, Navbar, Paper, Stack, Text, TextInput } from "@mantine/core";
import { SpotlightProvider } from "@mantine/spotlight";
import {
    ConfirmLeaveStaySave, ConfirmLeaveStaySaveResult, Link, MenuCardGrid, MenuCardItem, MenuCardRoot, MenuConfigItem, MenuNavBar, NavigationProtectionMode,
    Route, useConfirmNavigation, useMenuContent, useMicroMRouter, useModal
} from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { IconAddressBook, IconCategory, IconChartBar, IconHome, IconSettings, IconTags, IconWorld } from "@tabler/icons-react";
import { ReactNode, useMemo, useState } from "react";
import { MenuCardBreadcrumbs } from "../../../micromlib/src/UI/MenuCard/MenuCardBreadcrumbs/MenuCardBreadcrumbs";
import { createMockClient } from "../mocks/mockClient";

const Placeholder = ({ title }: { title: string }) => <Paper withBorder p="lg"><Text fw={600}>{title}</Text><Text size="sm" color="dimmed">Menu item content</Text></Paper>;

const demoMenus: Record<string, MenuConfigItem> = {
    main: {
        isMain: true,
        menu: () => [
            { ID: "home", label: "Home", icon: <IconHome size="1.2rem" />, section: "header", content: <Placeholder title="Home" /> },
            {
                ID: "sales", label: "Sales", icon: <IconChartBar size="1.2rem" />, section: "items", description: "Customers and reports", subitems: [
                    { ID: "customers", label: "Customers", icon: <IconAddressBook size="1.2rem" />, section: "items", canShowAsShortcut: true, description: "Customer list", content: <Placeholder title="Customers" /> },
                    { ID: "reports", label: "Reports", icon: <IconChartBar size="1.2rem" />, section: "items", notifications: 3, content: <Placeholder title="Reports" /> },
                ]
            },
            {
                ID: "settings", label: "Settings", icon: <IconSettings size="1.2rem" />, section: "items", subitems: [
                    { ID: "categories", label: "Categories", icon: <IconCategory size="1.2rem" />, section: "items", content: <Placeholder title="Categories" /> },
                    { ID: "tags", label: "Tags", icon: <IconTags size="1.2rem" />, section: "items", content: <Placeholder title="Tags" /> },
                    { ID: "geo", label: "Geography", icon: <IconWorld size="1.2rem" />, section: "items", content: <Placeholder title="Geography" /> },
                ]
            },
        ],
    },
};

function useDemoMenu() {
    const client = useMemo(() => createMockClient(), []);
    const [content, setContent] = useState<ReactNode>(null);
    const [, setOpened] = useState(false);
    const [isLoggedIn, setIsLoggedIn] = useState<boolean | undefined>(true);
    const result = useMenuContent({ menus: demoMenus, client, setContent, setOpened, isLoggedIn, setIsLoggedIn, enableMenuSecurity: false });
    return { ...result, content, setContent, setOpened };
}

function RouteBadge() {
    const { route } = useMicroMRouter();
    return <Text size="xs" color="dimmed">Route: <Code>{route || "/"}</Code></Text>;
}

const meta = {
    title: "MicroM/Menus",
    parameters: { controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

function NavBarStory() {
    const { mainMenu, content, setContent, setOpened } = useDemoMenu();
    return (
        <SpotlightProvider actions={mainMenu.actions}>
            <Group align="flex-start" noWrap>
                <Navbar width={{ base: 260 }} p="xs" h="70vh" sx={{ position: "static" }}>
                    <MenuNavBar items={mainMenu.items} setContent={setContent} setOpened={setOpened} clearContent
                        activeIDState={mainMenu.activeIDState} subitemActiveIDState={mainMenu.subitemActiveIDState} />
                </Navbar>
                <Stack sx={{ flex: 1 }}><RouteBadge />{content}</Stack>
            </Group>
        </SpotlightProvider>
    );
}

export const NavBar: Story = { name: "MenuNavBar", render: () => <NavBarStory />, parameters: { microm: { initialRoute: "/main/home" } } };

function CardsStory() {
    const { mainMenu } = useDemoMenu();
    const sales = mainMenu.items.find(i => i.ID === "sales")!;
    return (
        <Stack>
            <Text fw={600}>MenuCardRoot (root items are headings, subitems are cards)</Text>
            <MenuCardRoot items={mainMenu.items.filter(i => i.subitems)} breadcrumbs={[]} rootID="main" />
            <Text fw={600} mt="lg">MenuCardGrid</Text>
            <MenuCardGrid items={sales.subitems ?? []} />
            <Text fw={600} mt="lg">MenuCardItem</Text>
            <Box maw="24rem"><MenuCardItem item={sales} /></Box>
            <RouteBadge />
        </Stack>
    );
}

export const Cards: Story = { name: "MenuCardRoot, MenuCardGrid and MenuCardItem", render: () => <CardsStory />, parameters: { microm: { initialRoute: "/main" } } };

export const Breadcrumbs: Story = {
    name: "MenuCardBreadcrumbs",
    render: () => (
        <Stack>
            <MenuCardBreadcrumbs menuID="main" items={[
                { title: "Home", path: "/#/main" }, { title: "Settings", path: "/#/main/settings" },
                { title: "Geography", path: "/#/main/settings/geo" }, { title: "Countries", path: "/#/main/settings/geo/countries" },
                { title: "Argentina", path: "/#/main/settings/geo/countries/AR" },
            ]} />
            <RouteBadge />
        </Stack>
    ),
};

function RouterStory() {
    return (
        <Stack>
            <Group>
                <Link to="/first">First</Link>
                <Link to="/second">Second</Link>
                <Link to="/third">Third</Link>
            </Group>
            <RouteBadge />
            <Route path="/first"><Placeholder title="First route" /></Route>
            <Route path="/second"><Placeholder title="Second route" /></Route>
            <Route path="/third"><Placeholder title="Third route" /></Route>
        </Stack>
    );
}

export const Router: Story = { name: "Route and Link", render: () => <RouterStory />, parameters: { microm: { initialRoute: "/first" } } };

function ProtectedEditor({ mode }: { mode: NavigationProtectionMode }) {
    const [value, setValue] = useState("");
    const [saved, setSaved] = useState("");
    useConfirmNavigation({
        mode,
        hasUnsavedChanges: () => value !== saved,
        onSave: async () => { setSaved(value); return true; },
    });
    return (
        <Stack>
            <TextInput label={`Type something, then click another route (mode: ${mode})`} value={value} onChange={e => setValue(e.currentTarget.value)} />
            <Text size="xs" color="dimmed">Saved value: <Code>{saved || "(empty)"}</Code></Text>
        </Stack>
    );
}

function NavigationProtectionStory() {
    const modal = useModal();
    const [result, setResult] = useState<ConfirmLeaveStaySaveResult>();
    return (
        <Stack>
            <Group>
                <Link to="/editor">Editor</Link>
                <Link to="/other">Other page</Link>
            </Group>
            <RouteBadge />
            <Route path="/editor"><ProtectedEditor mode="confirm" /></Route>
            <Route path="/other"><Placeholder title="Other page" /></Route>
            <Group mt="lg">
                <Button variant="light" onClick={() => modal.open({
                    history: false,
                    modalProps: { title: "ConfirmLeaveStaySave" },
                    content: <ConfirmLeaveStaySave onResult={async r => { setResult(r); await modal.close(); }} />,
                })}>Open ConfirmLeaveStaySave</Button>
                <Text size="sm">Result: <Code>{result ?? "(none)"}</Code></Text>
            </Group>
        </Stack>
    );
}

export const NavigationProtection: Story = {
    name: "useConfirmNavigation and ConfirmLeaveStaySave",
    render: () => <NavigationProtectionStory />,
    parameters: { microm: { initialRoute: "/editor" } },
};
