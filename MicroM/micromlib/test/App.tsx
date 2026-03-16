import { ActionIcon, AppShell, DirectionProvider, Group, MantineProvider, NavLink, Text, useComputedColorScheme, useMantineColorScheme } from '@mantine/core';
import { IconMoonStars, IconSun } from '@tabler/icons-react';
import { ModalsManager } from 'UI';
import { ReactNode, useState } from 'react';
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
    const [content, setContent] = useState(<ContentPlaceholder></ContentPlaceholder>);

    return (
        <DirectionProvider>
            <MantineProvider defaultColorScheme="auto">
                <ModalsManager animationDuration={0} modalProps={{}}>
                    <AppShell padding="md" navbar={{ width: 300, breakpoint: 'sm' }} header={{ height: { base: 50, md: 70 } }}>
                    <AppShell.Navbar p="xs">
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
                    </AppShell.Navbar>

                    <AppShell.Header>
                        <Group style={{ height: '100%', display: "flex", justifyContent: "flex-start" }} px={20} justify="space-between">
                            <Text fw="700">Test micromlib</Text>
                            <Group style={{ flex: 1, justifyContent: "flex-end" }}>
                                <ThemeToggleButton />
                            </Group>
                        </Group>
                    </AppShell.Header>

                    <AppShell.Main>{content}</AppShell.Main>
                    </AppShell>
                </ModalsManager>
            </MantineProvider>
        </DirectionProvider>
    );
}

function ThemeToggleButton(): ReactNode {
    const { setColorScheme } = useMantineColorScheme();
    const computedColorScheme = useComputedColorScheme(undefined, { getInitialValueInEffect: true });

    return (
        <ActionIcon
            variant="default"
            onClick={() => setColorScheme(computedColorScheme === 'dark' ? 'light' : 'dark')}
            size={30}
            aria-label="Toggle color scheme"
        >
            {computedColorScheme === 'dark' ? <IconSun size="1rem" /> : <IconMoonStars size="1rem" />}
        </ActionIcon>
    );
}


