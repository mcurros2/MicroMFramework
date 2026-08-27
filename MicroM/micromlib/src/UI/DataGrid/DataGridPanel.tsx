import { Card, CardProps, DefaultMantineColor, Group, Loader, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import type { ReactNode } from "react";
import { DataGrid, DataGridProps } from ".";
import { MicroMClient } from "../../client/MicromClient";
import { ValuesObject } from "../../client/client.types";
import type { EntityGridBuilderProps, EntityGridSourceProps } from "../GetEntity/GetEntity";
import { useResolvedEntityBuilder } from "../GetEntity/useResolvedEntityBuilder";
import { useRetainedSearch } from "./useRetainedSearch";

type DataGridPanelBaseProps = Omit<DataGridProps, "entity" | "title"> & {
    client: MicroMClient;
    parentKeys?: ValuesObject;
    retainSearch?: string;
    onSearchTextChange?: DataGridProps["onSearch"];
    bgLight?: DefaultMantineColor;
    bgDark?: DefaultMantineColor;
    /** Props for the wrapping Card. Replaces the default object entirely when set per instance */
    containerCardProps?: Omit<CardProps, 'children'>;
    /** Component shown while the entity is being resolved */
    loadingComponent?: ReactNode;
};

export type DataGridPanelProps = DataGridPanelBaseProps & EntityGridSourceProps;

export const DataGridPanelDefaultProps: Partial<DataGridPanelProps> = {
    actionsButtonVariant: 'light',
    toolbarIconVariant: 'light',
    bgLight: 'gray.3',
    bgDark: undefined,
    gridHeight: 'flex-grow',
    containerCardProps: { h: '100%', withBorder: true },
    loadingComponent: <Group h="100%" align="flex-start"><Loader /></Group>,
};

export function DataGridPanel(props: DataGridPanelProps) {

    const mergedProps = useComponentDefaultProps('DataGridPanel', DataGridPanelDefaultProps, props);
    const {
        client, parentKeys, actionsButtonVariant, toolbarIconVariant, formMode, enableAdd, enableEdit, enableDelete, enableView,
        retainSearch, search, refreshOnInit, onSearch, onSearchTextChange, bgLight, bgDark, gridHeight, containerCardProps, loadingComponent,
        entityConstructor, entityLoader, ...rest
    } = mergedProps;

    const theme = useMantineTheme();

    const { result: entityBuilder, ready: entityReady } = useResolvedEntityBuilder<EntityGridBuilderProps>(client, parentKeys, mergedProps);

    const retainedSearchProps = useRetainedSearch({ retainSearch, search, refreshOnInit, onSearch, onSearchTextChange });

    return (
        <Card bg={theme.colorScheme === "light" ? bgLight : bgDark} {...containerCardProps}>
            {!entityReady || !entityBuilder ? (
                loadingComponent
            ) : (
                <DataGrid
                    {...rest}
                    {...retainedSearchProps}
                    parentKeys={parentKeys}
                    formMode={formMode}
                    enableAdd={enableAdd !== undefined ? enableAdd : (formMode === "view" ? false : undefined)}
                    enableEdit={enableEdit !== undefined ? enableEdit : (formMode === "view" ? false : undefined)}
                    enableDelete={enableDelete !== undefined ? enableDelete : (formMode === "view" ? false : undefined)}
                    enableView={enableView !== undefined ? enableView : (formMode === "view" ? true : undefined)}
                    actionsButtonVariant={actionsButtonVariant}
                    toolbarIconVariant={toolbarIconVariant}
                    entity={entityBuilder.entity}
                    viewName={entityBuilder.view}
                    gridHeight={gridHeight}
                />
            )}
        </Card>
    );
}
