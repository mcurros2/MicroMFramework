import { ActionIcon, AppShell, Burger, Button, Center, Group, Header, MediaQuery, Navbar, Stack, Text, Title, useMantineColorScheme, useMantineTheme } from "@mantine/core";
import { SpotlightProvider } from "@mantine/spotlight";
import { MenuCardNavBarPanel, MenuNavBarPanel, MenuRouteHost, useMenuContent } from "@mcurros2/microm";
import type { Meta, StoryObj } from "@storybook/react-vite";
import { IconLayoutGrid, IconMoonStars, IconSearch, IconSun } from "@tabler/icons-react";
import { ReactNode, useMemo, useState } from "react";
import { createMockClient } from "../mocks/mockClient";
import { cardMenus, navbarMenus } from "./menus";

function ColorSchemeToggle() {
    const { colorScheme, toggleColorScheme } = useMantineColorScheme();
    return (
        <ActionIcon variant="default" onClick={() => toggleColorScheme()} size={30} title="Toggle color scheme">
            {colorScheme === "dark" ? <IconSun size="1rem" /> : <IconMoonStars size="1rem" />}
        </ActionIcon>
    );
}

function LoggedOut({ onLogin }: { onLogin: () => void }) {
    return (
        <Center h="60vh">
            <Stack align="center">
                <Text>You are logged out.</Text>
                <Button onClick={onLogin}>Log in again</Button>
            </Stack>
        </Center>
    );
}

function NavbarApp() {
    const client = useMemo(() => createMockClient(), []);
    const theme = useMantineTheme();
    const [content, setContent] = useState<ReactNode>(null);
    const [opened, setOpened] = useState(false);
    const [isLoggedIn, setIsLoggedIn] = useState<boolean | undefined>(true);

    const { mainMenu } = useMenuContent({
        menus: navbarMenus, client, setContent, setOpened, isLoggedIn, setIsLoggedIn, enableMenuSecurity: false,
    });

    return (
        <SpotlightProvider actions={mainMenu.actions} searchIcon={<IconSearch size="1.2rem" />} nothingFoundMessage="No results" searchPlaceholder="Search menu...">
            <AppShell
                padding="md"
                navbarOffsetBreakpoint="sm"
                navbar={
                    <Navbar width={{ sm: 260 }} p="xs" hiddenBreakpoint="sm" hidden={!opened}>
                        <MenuNavBarPanel menuItems={mainMenu.items} setContent={setContent} setOpened={setOpened}
                            activeIDState={mainMenu.activeIDState} subitemActiveIDState={mainMenu.subitemActiveIDState} />
                    </Navbar>
                }
                header={
                    <Header height={56} px="md">
                        <Group h="100%" position="apart">
                            <Group>
                                <MediaQuery largerThan="sm" styles={{ display: "none" }}>
                                    <Burger opened={opened} onClick={() => setOpened(o => !o)} size="sm" />
                                </MediaQuery>
                                <Title order={4}>Sample app</Title>
                                <Text size="xs" color="dimmed">MenuNavBarPanel + useMenuContent</Text>
                            </Group>
                            <ColorSchemeToggle />
                        </Group>
                    </Header>
                }
                styles={{ main: { backgroundColor: theme.colorScheme === "dark" ? theme.colors.dark[8] : theme.colors.gray[0] } }}
            >
                {isLoggedIn ? content : <LoggedOut onLogin={() => setIsLoggedIn(true)} />}
            </AppShell>
        </SpotlightProvider>
    );
}

function CardsApp({ mode }: { mode: "icons" | "text" }) {
    const client = useMemo(() => createMockClient(), []);
    const theme = useMantineTheme();
    const [, setContent] = useState<ReactNode>(null);
    const [, setOpened] = useState(false);
    const [isLoggedIn, setIsLoggedIn] = useState<boolean | undefined>(true);

    const { menus, mainMenuId, mainMenu } = useMenuContent({
        menus: cardMenus, client, setContent, setOpened, isLoggedIn, setIsLoggedIn, enableMenuSecurity: false,
    });

    return (
        <SpotlightProvider actions={mainMenu.actions} searchIcon={<IconSearch size="1.2rem" />} nothingFoundMessage="No results" searchPlaceholder="Search menu...">
            <AppShell
                padding="md"
                navbar={<MenuCardNavBarPanel mode={mode} isLoggedIn={isLoggedIn} rootItems={mainMenu.items} homeMenuId={mainMenuId}
                    Logo={<IconLayoutGrid size="1.6rem" />} logoTooltip="Sample app" />}
                header={
                    <Header height={56} px="md">
                        <Group h="100%" position="apart">
                            <Group>
                                <Title order={4}>Sample app</Title>
                                <Text size="xs" color="dimmed">MenuRouteHost + MenuCardNavBarPanel</Text>
                            </Group>
                            <ColorSchemeToggle />
                        </Group>
                    </Header>
                }
                styles={{ main: { backgroundColor: theme.colorScheme === "dark" ? theme.colors.dark[8] : theme.colors.gray[0] } }}
            >
                <MenuRouteHost client={client} menuConfig={cardMenus} menus={menus} emptyNodeMessage={<Text color="dimmed">Nothing here.</Text>} />
            </AppShell>
        </SpotlightProvider>
    );
}

const meta = {
    title: "MicroM/Sample apps",
    parameters: { layout: "fullscreen", controls: { disable: true } },
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const NavbarMenu: Story = {
    name: "Navbar menu",
    render: () => <NavbarApp />,
    parameters: { microm: { initialRoute: "/main/home" } },
};

export const MenuCards: Story = {
    name: "Menu cards (icons)",
    render: () => <CardsApp mode="icons" />,
    parameters: { microm: { initialRoute: "/main" } },
};

export const MenuCardsText: Story = {
    name: "Menu cards (text)",
    render: () => <CardsApp mode="text" />,
    parameters: { microm: { initialRoute: "/main" } },
};
