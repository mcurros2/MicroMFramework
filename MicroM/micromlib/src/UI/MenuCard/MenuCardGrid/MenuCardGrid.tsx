import { SimpleGrid, SimpleGridProps, useComponentDefaultProps } from "@mantine/core";
import { MenuItem } from "../../Menu/MenuItem";
import { MenuCardItem } from "../MenuCardItem/MenuCardItem";

export interface MenuCardGridProps extends Omit<SimpleGridProps, 'children'> {
    items: MenuItem[];
    routeBuilder?: (path: string) => string;
}

export const MenuCardGridDefaultProps: Partial<MenuCardGridProps> = {
    routeBuilder: path => path,
    cols: 3,
    h: '100%',
    breakpoints: [
        { maxWidth: '74.375rem', cols: 2, spacing: 'sm' },
        { maxWidth: '51.438rem', cols: 1, spacing: 'sm' },
    ],
};

export function MenuCardGrid(props: MenuCardGridProps) {
    const { items, routeBuilder, ...others } =
        useComponentDefaultProps('MenuCardGrid', MenuCardGridDefaultProps, props);

    return (
        <SimpleGrid {...others}>
            {items.map(item => (
                <MenuCardItem key={`ci${item.ID}`} item={item} routeBuilder={routeBuilder} />
            ))}
        </SimpleGrid>
    );
}
