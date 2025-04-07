import { useComponentDefaultProps } from "@mantine/core";
import { IconProps, IconSearch } from "@tabler/icons-react";
import { Dispatch, ReactNode, SetStateAction } from "react";
import { MenuItem } from "./MenuItem";
import { MenuNavBar } from "./MenuNavBar";


export interface MenuPanelProps {
    setContent: Dispatch<SetStateAction<ReactNode>>,
    setOpened: Dispatch<SetStateAction<boolean>>,
    menuItems: MenuItem[],
    activeIDState: [string, Dispatch<SetStateAction<string>>],
    subitemActiveIDState: [string, Dispatch<SetStateAction<string>>],
    showSearch?: boolean,
    searchLabel?: string,
    searchIcon?: (props: IconProps) => ReactNode,
    autoCloseOnItemClickWhenSmallScreen?: boolean
}

export const MenuPanelDefaultProps: Partial<MenuPanelProps> = {
    showSearch: true,
    searchLabel: "Search",
    searchIcon: IconSearch,
    autoCloseOnItemClickWhenSmallScreen: true
}

export function MenuNavBarPanel(props: MenuPanelProps) {
    const {
        setContent, menuItems, activeIDState, subitemActiveIDState, showSearch, searchLabel, searchIcon, setOpened, autoCloseOnItemClickWhenSmallScreen
    } = useComponentDefaultProps('MenuNavBarPanel', MenuPanelDefaultProps, props);

    return (
        <MenuNavBar
            showSearch={showSearch}
            searchLabel={searchLabel}
            searchIcon={searchIcon}
            items={menuItems}
            setContent={setContent}
            setOpened={setOpened}
            clearContent={true}
            activeIDState={activeIDState}
            subitemActiveIDState={subitemActiveIDState}
            autoCloseOnItemClickWhenSmallScreen={autoCloseOnItemClickWhenSmallScreen}
        />
    )
}