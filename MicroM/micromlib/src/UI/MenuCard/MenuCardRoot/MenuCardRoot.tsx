import { Stack, StackProps, useComponentDefaultProps } from "@mantine/core";
import { Fragment } from "react/jsx-runtime";
import { MenuItem } from "../../Menu/MenuItem";
import { normalizeRouteURL } from "../../Router/MicroMRouterState";
import { MenuCardBreadcrumbsProps } from "../MenuCardBreadcrumbs/MenuCardBreadcrumbProps";
import { MenuCardBreadcrumbs } from "../MenuCardBreadcrumbs/MenuCardBreadcrumbs";
import { MenuCardGrid } from "../MenuCardGrid/MenuCardGrid";

export interface MenuCardRootProps extends Omit<StackProps, 'children'> {
    items: MenuItem[],
    breadcrumbs: MenuCardBreadcrumbsProps['items'],
    rootID: string,
    renderBreadcrumbs?: boolean,
    routeBuilder?: (path: string) => string
}

export const MenuCardRootDefaultProps: Partial<MenuCardRootProps> = {
    renderBreadcrumbs: true,
    routeBuilder: path => path,
    spacing: 'xl',
};

export function MenuCardRoot(props: MenuCardRootProps) {
    const {
        items, rootID, breadcrumbs, renderBreadcrumbs, routeBuilder, ...others
    } = useComponentDefaultProps('MenuCardRoot', MenuCardRootDefaultProps, props);

    if (items.length === 0) return null;

    return (
        <Stack {...others} key={`crt${rootID}`}>
            {items.map((value) => (
                <Fragment key={`mcb${value.ID}`}>
                    {renderBreadcrumbs && breadcrumbs.length > 0 &&
                        <MenuCardBreadcrumbs items={breadcrumbs} menuID={value.ID} />
                    }
                    {renderBreadcrumbs && breadcrumbs.length === 0 &&
                        <MenuCardBreadcrumbs items={[{ title: value.label, path: normalizeRouteURL(routeBuilder!(value.menuPath || '')) }]} menuID={value.ID} />
                    }
                    <MenuCardGrid items={value.subitems || []} routeBuilder={routeBuilder} />
                </Fragment>
            ))}
        </Stack>
    )
}
