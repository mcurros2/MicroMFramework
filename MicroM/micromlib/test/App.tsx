import { ActionIcon, AppShell, ColorScheme, ColorSchemeProvider, Group, Header, MantineProvider, NavLink, Navbar, Text } from '@mantine/core';
import { IconMoonStars, IconSun } from '@tabler/icons-react';
import { ModalsManager } from 'UI';
import { useState } from 'react';
import { AddressInputTest } from './AddressInputTest/AddressInputTest';
import { AddressLookupTest } from './AddressInputTest/AddressLookupTest';
import { AvatarUploaderTest } from './AvatarUploadTest/AvatarUploaderTest';
import { ContentPlaceholder } from './ContentPlaceholder';
import { FileUploaderTest } from './FileUploaderTest/FileUploaderTest';
import { GoogleMapsAPIKey } from './GoogleMapsAPIKey/GoogleMapsAPIKey';
import { AncestorResizeTest } from './GoogleMapsTest/AcenstorResizeTest';
import { AddressAutocompleteTest } from './GoogleMapsTest/AddressAutocompleteTest';
import { AddressSearchTest } from './GoogleMapsTest/AddressSearchTest';
import { RegionSelectorTest } from './GoogleMapsTest/CountySelectorTest';
import { GridTest } from './GridTest/GridTest';
import { RingProgressFieldTest } from './StatsTest/RingProgressFieldTest';

export function App() {
    const [colorScheme, setColorScheme] = useState<ColorScheme>('light');
    const toggleColorScheme = (value?: ColorScheme) => setColorScheme(value || (colorScheme === 'dark' ? 'light' : 'dark'));

    const [content, setContent] = useState(<ContentPlaceholder></ContentPlaceholder>);

    return (
        <ColorSchemeProvider colorScheme={colorScheme} toggleColorScheme={toggleColorScheme}>
            <MantineProvider theme={{ colorScheme }} withGlobalStyles withNormalizeCSS>
                <ModalsManager animationDuration={0} modalProps={{}}>
                    <AppShell
                        padding="md"
                        navbar={
                            <Navbar width={{ base: 300 }} height={500} p="xs">
                                <Navbar.Section>
                                    <NavLink onClick={() => setContent(<GoogleMapsAPIKey />)} label="Set google maps API KEY" />
                                    <NavLink onClick={() => setContent(<GridTest></GridTest>)} label="Grid" />
                                    <NavLink onClick={() => setContent(<AddressAutocompleteTest></AddressAutocompleteTest>)} label="Address autocomplete" />
                                    <NavLink onClick={() => setContent(<AddressInputTest></AddressInputTest>)} label="Address input" />
                                    <NavLink onClick={() => setContent(<AddressSearchTest></AddressSearchTest>)} label="Address search" />
                                    <NavLink onClick={() => setContent(<AddressLookupTest />)} label="Address lookup" />
                                    <NavLink onClick={() => setContent(<FileUploaderTest></FileUploaderTest>)} label="File uploader" />
                                    <NavLink onClick={() => setContent(<AvatarUploaderTest></AvatarUploaderTest>)} label="Avatar uploader" />
                                    <NavLink onClick={() => setContent(<RegionSelectorTest />)} label="Google region selector" />
                                    <NavLink onClick={() => setContent(<RingProgressFieldTest />)} label="Stats test" />
                                    <NavLink onClick={() => setContent(<AncestorResizeTest />)} label="Ancestor resize test" />
                                </Navbar.Section>
                            </Navbar>}
                        header={
                            <Header height={{ base: 50, md: 70 }}>
                                <Group sx={{ height: '100%', display: "flex", justifyContent: "flex-start" }} px={20} position="apart">
                                    <Text fw="700">Test micromlib</Text>
                                    <Group sx={{ flex: 1, justifyContent: "flex-end" }}>
                                        <ActionIcon variant="default" onClick={() => toggleColorScheme()} size={30}>
                                            {colorScheme === 'dark' ? <IconSun size="1rem" /> : <IconMoonStars size="1rem" />}
                                        </ActionIcon>
                                    </Group>
                                </Group>
                            </Header>
                        }
                    >
                        {content}
                    </AppShell>
                </ModalsManager>
            </MantineProvider>
        </ColorSchemeProvider>
    );
}