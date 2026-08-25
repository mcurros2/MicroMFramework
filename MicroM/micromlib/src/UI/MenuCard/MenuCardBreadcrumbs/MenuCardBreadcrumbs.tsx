import { Anchor, Breadcrumbs, Menu, Text, Title, useComponentDefaultProps } from "@mantine/core";
import { forwardRef } from "react";
import { MenuCardBreadcrumbsProps } from "./MenuCardBreadcrumbProps";

export const MenuCardBreadcrumbsDefaultProps: Partial<MenuCardBreadcrumbsProps> = {
    separator: "/",
    maxItems: 4,
    titleSize: 5,
    textColor: 'gray',
    collapsedIndicator: '...',
    collapsedMenuProps: { shadow: 'md', width: 200, trigger: 'hover' },
};

export const MenuCardBreadcrumbs = forwardRef<HTMLDivElement, MenuCardBreadcrumbsProps>(function MenuCardBreadcrumbs(props: MenuCardBreadcrumbsProps, ref) {
    const { items, menuID, maxItems, titleSize, textColor, collapsedIndicator, collapsedMenuProps, ...rest } = useComponentDefaultProps('MenuCardBreadcrumbs', MenuCardBreadcrumbsDefaultProps, props);

    const renderBreadcrumbItem = (item: any) => (
        <Anchor key={`bc${item.path}${menuID}`} href={item.path} color={textColor}>
            <Title order={titleSize}>{item.title}</Title>
        </Anchor>
    );

    let breadcrumbElements: React.ReactNode[] = [];

    if (items.length <= maxItems!) {
        breadcrumbElements = items.map(renderBreadcrumbItem);
    } else {
        const firstItem = items[0];
        const collapsedItems = items.slice(1, items.length - 2);
        const lastTwoItems = items.slice(items.length - 2);

        breadcrumbElements = [
            renderBreadcrumbItem(firstItem),

            <Menu key={`bc-collapsed-${menuID}`} {...collapsedMenuProps}>
                <Menu.Target>
                    <Text color={textColor} sx={{ cursor: 'pointer', display: 'flex', alignItems: 'center' }}>
                        <Title order={titleSize}>{collapsedIndicator}</Title>
                    </Text>
                </Menu.Target>
                <Menu.Dropdown>
                    {collapsedItems.map(item => (
                        <Menu.Item key={`menu${item.path}${menuID}`} component="a" href={item.path}>
                            {item.title}
                        </Menu.Item>
                    ))}
                </Menu.Dropdown>
            </Menu>,

            ...lastTwoItems.map(renderBreadcrumbItem)
        ];
    }

    return (
        <Breadcrumbs {...rest} ref={ref}>
            {breadcrumbElements}
        </Breadcrumbs>
    );
});