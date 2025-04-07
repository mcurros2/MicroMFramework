import { Badge, Group, NavLink, Skeleton, rem, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { Dispatch, ReactNode, SetStateAction, useCallback, useEffect } from "react";
import { isPromise } from "../../Entity";
import { useMicroMRouter } from "../Router/useMicroMRouter";
import { MenuItem } from "./MenuItem";


export interface MenuItemsProps {
    AllItems: MenuItem[],
    sectionItems: MenuItem[],
    setContent: Dispatch<SetStateAction<ReactNode>>,
    setOpened: Dispatch<SetStateAction<boolean>>,
    clearContent: boolean,
    activeID: string,
    subitemActiveID: string,
    showSubitemIcons?: boolean,
    defaultLoadingComponent?: ReactNode,
    onActiveChange: React.Dispatch<SetStateAction<string>>,
    onSubitemActiveChange: React.Dispatch<SetStateAction<string>>,
    showItemDescription?: boolean,
    autoHideNavBarOnClick?: boolean
}

export const MenuItemsPropsDefaultProps: Partial<MenuItemsProps> = {
    defaultLoadingComponent: <Skeleton />
}

export function MenuNavLinks(props: MenuItemsProps) {

    const {
        AllItems, sectionItems, setContent, clearContent, activeID, subitemActiveID, defaultLoadingComponent, onActiveChange, onSubitemActiveChange,
        showSubitemIcons, showItemDescription, setOpened, autoHideNavBarOnClick
    } = useComponentDefaultProps('MenuItems', MenuItemsPropsDefaultProps, props);

    const theme = useMantineTheme();

    const { navigate, path } = useMicroMRouter();

    const setActiveContent = useCallback(async (menuItem: MenuItem) => {
        if (menuItem.content) {
            if (isPromise(menuItem.content)) {
                setContent(defaultLoadingComponent);
                const content = await menuItem.content;
                setContent(content);
            } else {
                setContent(menuItem.content);
            }
        } else if (clearContent) {
            setContent(<></>);
        }

        if (menuItem.onClick) {
            menuItem.onClick();
        }

    }, [clearContent, defaultLoadingComponent, setContent]);


    // Handler to process navigation based on the current path. This renders the content for navigated item
    useEffect(() => {
        const pathParts = path.split('/').filter(part => part.trim() !== '');
        const [itemId, subitemId] = pathParts;

        const item = AllItems.find(it => it.ID === itemId);
        if (item) {
            if (!item.noActive) {
                onActiveChange(item.ID);
            }

            if (subitemId && item.subitems) {
                const subitem = item.subitems.find(si => si.ID === subitemId);
                if (subitem) {
                    onSubitemActiveChange(subitem.ID);
                    setActiveContent(subitem);
                } else {
                    // If subitem ID was specified but not found, clear subitemActiveID
                    onSubitemActiveChange('');
                }
            } else {
                // setContent only on las level, if we have subitems just expand the menu
                if (!item.subitems) setActiveContent(item);

                onSubitemActiveChange(''); // Ensure subitemActiveID is cleared
            }
        } else if (clearContent) {
            // Path does not match any item
            setContent(<></>);
            onActiveChange('');
            onSubitemActiveChange('');
        }
    }, [clearContent, AllItems, onActiveChange, onSubitemActiveChange, path, setActiveContent, setContent]);


    // Route for navigation
    const handleItemClick = (item: MenuItem, subitem?: MenuItem) => {
        const route = subitem ? `/${item.ID}/${subitem.ID}` : `/${item.ID}`;
        navigate(`${route}`);

        if (autoHideNavBarOnClick && !item.subitems) setOpened(false);
    };

    return (
        <>{
            sectionItems.map((item: MenuItem) => (
                <NavLink
                    key={item.ID}
                    label={
                        (item.notifications && (item.subitems || item.rightSection)) ? <Group><Group sx={{ flex: 1 }}>{item.labelComponent ?? item.label}</Group><Badge sx={{ justifySelf: "flex-end" }} variant="filled" size="xs">{item.notifications}</Badge></Group> : (item.labelComponent ?? item.label)
                    }
                    icon={item.icon}
                    rightSection={(item.notifications && !item.subitems && !item.rightSection) ? <Badge variant="filled" size="xs">{item.notifications}</Badge> : item.rightSection}
                    description={showItemDescription && item.description}
                    onClick={() => handleItemClick(item)}
                    active={item.noActive !== true && item.ID === activeID}
                    opened={(item.noActive !== true && item.ID === activeID && item.subitems && item.subitems.some(subitem => subitem.noActive !== true && subitem.ID === subitemActiveID)) || undefined}
                >
                    {
                        item.subitems && item.subitems.map((subitem) => {
                            return <NavLink
                                sx={{
                                    borderLeft: `${rem(1)} solid ${theme.colorScheme === 'dark' ? theme.colors.dark[4] : theme.colors.gray[3]}`
                                }}
                                pl="1.8rem"
                                key={item.ID + subitem.ID}
                                label={subitem.label}
                                icon={showSubitemIcons && subitem.icon}
                                rightSection={subitem.notifications && <Badge variant="filled" size="xs">{subitem.notifications}</Badge>}
                                description={showItemDescription && subitem.description}
                                onClick={() => handleItemClick(item, subitem)}
                                active={subitem.noActive !== true && subitem.ID === subitemActiveID}
                            />
                        })
                    }

                </NavLink>
            ))

        }</>
    );

}