import { AppShell, NavLink, ScrollArea, useProps, useMantineTheme } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { openSpotlight } from "@mantine/spotlight";
import { IconProps, IconSearch } from "@tabler/icons-react";
import { Dispatch, ReactNode, SetStateAction, useMemo } from "react";
import { MenuItem } from "./MenuItem";
import { MenuNavLinks } from "./MenuNavLinks";

export interface MenuProps {
    items: MenuItem[],
    setContent: Dispatch<SetStateAction<ReactNode>>,
    setOpened: Dispatch<SetStateAction<boolean>>,
    clearContent: boolean,
    activeIDState: [string, Dispatch<SetStateAction<string>>],
    subitemActiveIDState: [string, Dispatch<SetStateAction<string>>],
    showSearch?: boolean,
    searchLabel?: string,
    searchIcon?: (props: IconProps) => ReactNode,
    autoCloseOnItemClickWhenSmallScreen?: boolean
}

export const MenuDefaultProps: Partial<MenuProps> = {
    showSearch: true,
    searchLabel: "Search",
    searchIcon: IconSearch,
    autoCloseOnItemClickWhenSmallScreen: true
}


export function MenuNavBar(props: MenuProps) {
    const {
        items, setContent, clearContent, activeIDState, subitemActiveIDState, showSearch, searchLabel, searchIcon, setOpened, autoCloseOnItemClickWhenSmallScreen
    } = useProps('MenuNavBar', MenuDefaultProps, props);

    const theme = useMantineTheme();
    const matches = useMediaQuery(`(max-width: ${theme.breakpoints.sm})`);

    const [activeID, setActiveID] = activeIDState;
    const [subitemActiveID, setSubitemActiveID] = subitemActiveIDState;

    const header_section = useMemo(() => items.filter((item) => item.section === 'header'), [items]);
    const items_section = useMemo(() => items.filter((item) => item.section === 'items'), [items]);
    const footer_section = useMemo(() => items.filter((item) => item.section === 'footer'), [items]);

    const SearchIcon = searchIcon!;

    const autoHideNavBarOnClick = autoCloseOnItemClickWhenSmallScreen && matches;

    return (
        <>
            <AppShell.Section>
                {showSearch &&
                    <NavLink
                        key="menu-Search"
                        label={searchLabel}
                        leftSection={<SearchIcon size="1.2rem" />}
                        onClick={() => {
                            openSpotlight();
                        }}
                        mb="xs"
                    />}
                <MenuNavLinks AllItems={items} sectionItems={header_section} autoHideNavBarOnClick={autoHideNavBarOnClick} setOpened={setOpened} setContent={setContent} clearContent={clearContent} activeID={activeID} onActiveChange={setActiveID} subitemActiveID={subitemActiveID} onSubitemActiveChange={setSubitemActiveID} />
            </AppShell.Section>

            <AppShell.Section grow component={ScrollArea} mx="-xs" px="xs">
                <MenuNavLinks AllItems={items} sectionItems={items_section} autoHideNavBarOnClick={autoHideNavBarOnClick} setOpened={setOpened} setContent={setContent} clearContent={clearContent} activeID={activeID} onActiveChange={setActiveID} subitemActiveID={subitemActiveID} onSubitemActiveChange={setSubitemActiveID} />
            </AppShell.Section>

            <AppShell.Section>
                <MenuNavLinks AllItems={items} sectionItems={footer_section} autoHideNavBarOnClick={autoHideNavBarOnClick} setOpened={setOpened} setContent={setContent} clearContent={clearContent} activeID={activeID} onActiveChange={setActiveID} subitemActiveID={subitemActiveID} onSubitemActiveChange={setSubitemActiveID} />
            </AppShell.Section>
        </>
    );
}

