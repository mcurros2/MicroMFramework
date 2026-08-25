import { Card, CardProps, DefaultMantineColor, Group, Loader, SimpleGrid, SimpleGridProps, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import type { ReactNode } from "react";
import { DataView, DataViewProps } from ".";
import { MicroMClient } from "../../client/MicromClient";
import { ValuesObject } from "../../client/client.types";
import { useRetainedSearch } from "../DataGrid/useRetainedSearch";
import type { EntityBuilderProps, EntityBuilderSourceProps } from "../GetEntity/GetEntity";
import { useResolvedEntityBuilder } from "../GetEntity/useResolvedEntityBuilder";

type DataViewPanelBaseProps = Omit<DataViewProps, "entity" | "title" | "viewName" | "Card" | "RowsContainer" | "RowsContainerProps"> & {
    client: MicroMClient;
    parentKeys?: ValuesObject;
    Card?: DataViewProps["Card"];
    retainSearch?: string;
    onSearchTextChange?: DataViewProps["onSearch"];
    bgLight?: DefaultMantineColor;
    bgDark?: DefaultMantineColor;
    rowsContainerProps?: SimpleGridProps;
    /** Props for the wrapping Card. Replaces the default object entirely when set per instance */
    containerCardProps?: Omit<CardProps, 'children'>;
    /** Component shown while the entity is being resolved */
    loadingComponent?: ReactNode;
};

export type DataViewPanelProps = DataViewPanelBaseProps & EntityBuilderSourceProps;

export const DataViewPanelDefaultProps: Partial<DataViewPanelProps> = {
    actionsButtonVariant: 'light',
    toolbarIconVariant: 'light',
    bgLight: 'gray.3',
    bgDark: undefined,
    rowsContainerProps: {
        cols: 3,
        h: "100%",
        breakpoints: [
            { maxWidth: '94.375rem', cols: 2, spacing: 'sm' },
            { maxWidth: '74.375rem', cols: 1, spacing: 'sm' },
            { maxWidth: '51.438rem', cols: 1, spacing: 'sm' },
        ],
    },
    containerCardProps: { h: '100%', withBorder: true },
    loadingComponent: <Group h="100%" align="flex-start"><Loader /></Group>,
};

export function DataViewPanel(props: DataViewPanelProps) {

    const mergedProps = useComponentDefaultProps('DataViewPanel', DataViewPanelDefaultProps, props);
    const {
        client, parentKeys, actionsButtonVariant, toolbarIconVariant, formMode, enableAdd, enableEdit, enableDelete, enableView, Card: CardProp,
        retainSearch, search, refreshOnInit, onSearch, onSearchTextChange, bgLight, bgDark, rowsContainerProps, containerCardProps, loadingComponent,
        entityConstructor, entityLoader, ...rest
    } = mergedProps;

    const theme = useMantineTheme();

    const { result: entityBuilder, ready: entityReady } =
        useResolvedEntityBuilder<EntityBuilderProps>(client, parentKeys, mergedProps);

    const retainedSearchProps = useRetainedSearch({ retainSearch, search, refreshOnInit, onSearch, onSearchTextChange });

    return (
        <Card bg={theme.colorScheme === "light" ? bgLight : bgDark} {...containerCardProps}>
            {!entityReady || !entityBuilder ? (
                loadingComponent
            ) : (
                <DataView
                    {...rest}
                    {...retainedSearchProps}
                    RowsContainer={SimpleGrid}
                    RowsContainerProps={rowsContainerProps}
                    formMode={formMode}
                    enableAdd={enableAdd !== undefined ? enableAdd : (formMode === "view" ? false : undefined)}
                    enableEdit={enableEdit !== undefined ? enableEdit : (formMode === "view" ? false : undefined)}
                    enableDelete={enableDelete !== undefined ? enableDelete : (formMode === "view" ? false : undefined)}
                    enableView={enableView !== undefined ? enableView : (formMode === "view" ? true : undefined)}
                    actionsButtonVariant={actionsButtonVariant}
                    toolbarIconVariant={toolbarIconVariant}
                    entity={entityBuilder.entity}
                    viewName={entityBuilder.view}
                    Card={entityBuilder.Card || CardProp}
                />
            )}
        </Card>
    );
}
