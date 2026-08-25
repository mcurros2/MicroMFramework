import { Stack, StackProps, useComponentDefaultProps } from "@mantine/core";
import { Fragment, isValidElement, ReactNode, useEffect, useState } from "react";
import { isPromise } from "../../../Entity/GenericFunctions";
import { MenuItem, MenuItemContext, resolveMenuItemContent } from "../../Menu/MenuItem";
import { MenuCardBreadcrumbsProps } from "../MenuCardBreadcrumbs/MenuCardBreadcrumbProps";
import { MenuCardBreadcrumbs } from "../MenuCardBreadcrumbs/MenuCardBreadcrumbs";

export interface MenuCardContentPanelProps extends Omit<StackProps, 'children'> {
    item: MenuItem,
    itemContext?: MenuItemContext,
    breadcrumbs: MenuCardBreadcrumbsProps['items'],
    renderBreadcrumbs?: boolean,
    emptyNodeMessage?: ReactNode,
    defaultLoadingComponent?: ReactNode
}

export const MenuCardContentPanelDefaultProps: Partial<MenuCardContentPanelProps> = {
    renderBreadcrumbs: true,
    spacing: 'xs',
    h: '100%',
};

export function MenuCardContentPanel(props: MenuCardContentPanelProps) {
    const {
        item, itemContext, breadcrumbs, renderBreadcrumbs, emptyNodeMessage, defaultLoadingComponent, ...others
    } = useComponentDefaultProps('MenuCardContentPanel', MenuCardContentPanelDefaultProps, props);

    const [panelContent, setPanelContent] = useState<ReactNode>(null);

    useEffect(() => {
        let mounted = true;

        const getContent = async () => {
            const content = resolveMenuItemContent(item, itemContext);
            if (isPromise(content)) {
                if (mounted) setPanelContent(item.loadingComponent || defaultLoadingComponent);
                const ret = await content;
                if (mounted) setPanelContent(ret);
                return;
            }
            if (mounted) setPanelContent(content as ReactNode);
        };
        getContent();

        return () => {
            mounted = false;
        };
    }, [defaultLoadingComponent, item, itemContext]);

    return (
        <Stack {...others}>
            {renderBreadcrumbs && breadcrumbs.length > 0 &&
                <MenuCardBreadcrumbs items={breadcrumbs} menuID={item.ID} />
            }
            {isValidElement(panelContent) && panelContent.type === Fragment && !panelContent.props.children ? emptyNodeMessage : panelContent}
        </Stack>
    );
}

