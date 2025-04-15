import { Box, Group, Select, SelectItem, Space, Text, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { useMemo, useRef, useState } from "react";
import { AlertError, FakeProgressBar, useExecuteView, useFirstVisible, useViewState } from "../../UI/Core";
import { DataViewLimitData } from "../DataView/DataView.types";
import { Grid } from "../Grid";
import { DataGridProps } from "./DataGrid.types";
import { DataGridActionsToolbar } from "./DataGridActionsToolbar";
import { DataGridColumnsMenu } from "./DataGridColumnsMenu";
import { DataGridToolbar } from "./DataGridToolbar";
import { useDataGrid } from "./useDatagrid";

export const DataGridDefaultProps: Partial<DataGridProps> = {
    search: [],
    limit: "10000",
    refreshOnInit: false,
    selectionMode: "multi",
    gridHeight: "auto",
    preserveSelection: true,
    autoSelectFirstRow: true,
    toolbarIconVariant: "light",
    actionsButtonVariant: "light",
    modalFormSize: "lg",
    enableAdd: true,
    enableEdit: true,
    enableDelete: true,
    enableExport: true,
    columnBorders: true,
    rowBorders: false,
    withBorder: true,
    autoSizeColumnsOnLoad: true,
    showActions: true,
    labels: {
        addLabel: "Add",
        editLabel: "Edit",
        deleteLabel: "Delete",
        viewLabel: "View",
        rowsReturnedLabel: "Rows returned:",
        selectedRowsLabel: "rows selected.",
        limitLabel: "Limit",
        rowsLabel: "rows",
        unlimitedLabel: "Unlimited",
        recordsLabel: "records",
        recordLabel: "record",
        AreYouSureLabel: "Are you sure?",
        YouWillDeleteLabel: "You will delete",
        warningLabel: "Warning",
        YouMustSelectOneOrMoreRecordsToDelete: "You must select one or more records to delete",
        closeLabel: "Close",
        YouMustSelectOneOrMoreRecordsToExecuteAction: "You must select one or more records to execute this action",
        YouMustSelectAtLeast: "You must select at least",
        YouMustSelectBetween: "You must select between",
        YouMustSelect: "You must select",
        YouMustSelectMaximum: "You must select a maximum of",
    },
    renderOnlyWhenVisible: true,
    filtersFormSize: "md",
    showToolbar: true,
    showActionsToolbar: true,
    doubleClickAction: "edit",
    showColumnsConfigMenu: true,
    withModalFullscreenButton: true,
    showSelectRowsButton: true
}

export function DataGrid(props: DataGridProps) {
    props = useComponentDefaultProps('DataGrid', DataGridDefaultProps, props);
    const {
        entity, selectionMode, gridHeight, preserveSelection, autoSelectFirstRow, autoFocus, toolbarIconVariant, actionsButtonVariant,
        enableAdd, enableEdit, enableDelete, enableView, enableExport, columnBorders, autoSizeColumnsOnLoad, rowBorders, withBorder,
        labels, columnsOverrides, toolbarSize, viewName, showActions, renderOnlyWhenVisible, filtersFormSize, parentKeys, search,
        limit, parentFormAPI, showToolbar, showActionsToolbar, enableImport, setInitialFiltersFromColumns, visibleFilters, formMode,
        showColumnsConfigMenu, showSelectRowsButton
    } = props;

    const theme = useMantineTheme();

    const visibilityDivRef = useRef<HTMLDivElement>(null);
    const isFirstVisible = useFirstVisible(visibilityDivRef);

    const [searchData, setSearchData] = useState<SelectItem[]>(search?.map(s => { return { value: s, label: s } }) as SelectItem[]);

    const viewState = useViewState(search, limit);

    const executeViewState = useExecuteView(entity, parentKeys, viewName, viewState.searchText, viewState.limitRows, viewState.refresh, viewState.filterValues);

    const dataGridAPI = useDataGrid(props, { executeViewState, setRefresh: viewState.setRefresh, setSearchText: viewState.setSearchText });

    const { isLoading, rows, columns, setColumns } = dataGridAPI;

    const effectiveColumnOverrides = useMemo(() => {
        if (!entity || !viewName) return columnsOverrides;
        return columnsOverrides ? columnsOverrides : entity?.def.views[viewName]?.gridColumnsOverrides?.(theme) ?? columnsOverrides;
    }, [columnsOverrides, entity, theme, viewName]);

    const [openColumnsConfigMenu, setOpenColumnsConfigMenu] = useState(false);

    const ConfigMenuDropDown = useMemo(() => (
        <DataGridColumnsMenu
            setOpened={setOpenColumnsConfigMenu}
            columns={columns}
            setColumns={setColumns}
        />
    ), [columns, setColumns]);

    return (
        <>
            {renderOnlyWhenVisible && isFirstVisible === false ?
                <div ref={visibilityDivRef} style={{ height: props.gridHeight }}></div>
                :
                <section>
                    {showToolbar &&
                        <DataGridToolbar
                            {...labels!}

                            enableExport={enableExport}
                            onExportClick={dataGridAPI.handleExport}

                            onRefreshClick={dataGridAPI.handleRefresh}
                            onCheckboxToggle={dataGridAPI.handleToggleSelectable}
                            autoFocus={autoFocus}
                            toolbarIconVariant={toolbarIconVariant}
                            size={toolbarSize}

                            searchText={viewState.searchText}
                            setSearchText={viewState.setSearchText}
                            searchData={searchData}
                            setSearchData={setSearchData}

                            client={entity?.API.client}

                            FiltersEntity={(entity && viewName) ? entity?.def.views[viewName].FiltersEntity : undefined}

                            filterValues={viewState.filterValues}
                            setFilterValues={viewState.setFilterValues}

                            filtersDescription={viewState.filtersDescription}
                            setFiltersDescription={viewState.setFiltersDescription}

                            filtersFormSize={filtersFormSize!}
                            onImportClick={dataGridAPI.handleImportDataClick}
                            enableImport={enableImport}

                            visibleFilters={visibleFilters}
                            initialColumnFilters={setInitialFiltersFromColumns && entity ? entity.def.columns : undefined}

                            showColumnsConfig={showColumnsConfigMenu}
                            configMenuOpened={openColumnsConfigMenu}
                            setConfigMenuOpened={setOpenColumnsConfigMenu}
                            configMenuDropdown={ConfigMenuDropDown}

                            showSelectRowsButton={showSelectRowsButton}
                        />
                    }
                    {showActionsToolbar &&
                        <>
                            <Space h="xs" />
                            <DataGridActionsToolbar
                                {...labels!}
                                size={toolbarSize}
                                viewName={viewName}

                                onAddClick={dataGridAPI.handleAddClick}
                                onEditClick={dataGridAPI.handleEditClick}
                                onDeleteClick={dataGridAPI.handleDeleteClick}
                                onViewClick={dataGridAPI.handleViewClick}

                                enableAdd={enableAdd}
                                enableEdit={enableEdit}
                                enableDelete={enableDelete}
                                enableView={enableView}

                                actionsButtonVariant={actionsButtonVariant}
                                clientActions={entity?.def.clientActions ?? {}}
                                handleExecuteAction={dataGridAPI.handleExecuteAction}
                                showActions={showActions}

                                parentFormMode={formMode || parentFormAPI?.formMode}

                            />
                        </>
                    }
                    <Box pos={"relative"} mt="sm" mb="sm" >
                        {isLoading &&
                            <FakeProgressBar pos={"absolute"} style={{ top: 0, width: "100%", zIndex: 999 }} size="xs" />
                        }
                        {!isLoading && executeViewState.error &&
                            <AlertError mb="xs">Error: {executeViewState.error.status} {executeViewState.error.message} {executeViewState.error.statusMessage}</AlertError>
                        }
                        <Grid
                            columns={columns}
                            rows={rows}
                            onDoubleClick={dataGridAPI.handleDoubleClick}
                            gridHeight={gridHeight === 'auto' ? '50vh' : gridHeight}
                            preserveSelection={preserveSelection}
                            onSelectionChanged={dataGridAPI.handleSelectionChanged}
                            showSelectCheckbox={dataGridAPI.showSelectCheckbox}
                            autoSelectFirstRow={autoSelectFirstRow}
                            selectionMode={selectionMode}
                            columnBorders={columnBorders}
                            autoSizeColumnsOnLoad={autoSizeColumnsOnLoad}
                            rowBorders={rowBorders}
                            withBorder={withBorder}
                            columnsOverrides={effectiveColumnOverrides}
                        />
                    </Box>
                    {showToolbar &&
                        <Group>
                            {executeViewState && !executeViewState.error &&
                                <>
                                    <Text fz="sm">{labels?.limitLabel}</Text>
                                    <Select size="xs" sx={{ maxWidth: '7rem' }}
                                        data={DataViewLimitData(labels!.rowsLabel, labels!.unlimitedLabel)}
                                        value={viewState.limitRows}
                                        onChange={viewState.setLimitRows}
                                    />
                                </>
                            }
                            {rows &&
                                <Text fz="xs" c="dimmed">{labels?.rowsReturnedLabel} {rows.length}{dataGridAPI.selectedRowsCount ? <>, {dataGridAPI.selectedRowsCount} {labels?.selectedRowsLabel}</> : ""}</Text>
                            }
                        </Group>
                    }
                </section>
            }
        </>
    );
}
