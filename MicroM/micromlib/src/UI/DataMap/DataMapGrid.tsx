import { Box, Group, Select, Text, useComponentDefaultProps } from "@mantine/core";
import { useRef } from "react";
import { DataResult, OperationStatus } from "../../client";
import { AlertError, FakeProgressBar, FormMode, useFirstVisible, useViewState } from "../Core";
import { DataGridDefaultProps, DataGridProps } from "../DataGrid";
import { DataGridActionsToolbar } from "../DataGrid/DataGridActionsToolbar";
import { useDataGrid } from "../DataGrid/useDatagrid";
import { DataViewLimitData } from "../DataView";
import { Grid, GridSelection } from "../Grid";


export interface DataMapGridProps extends DataGridProps {
    dataGridAPI: ReturnType<typeof useDataGrid>,
    executeViewState: OperationStatus<DataResult[]>,
    viewState: ReturnType<typeof useViewState>,
    selectedRows: GridSelection,
    setSelectedRows?: (rows: GridSelection) => void,
    formMode?: FormMode
}

export const DataMapGridDefaultProps: Partial<DataMapGridProps> = DataGridDefaultProps;

export function DataMapGrid(props: DataMapGridProps) {
    const {
        labels, toolbarSize, viewName, preserveSelection, autoSelectFirstRow, selectionMode, columnBorders, autoSizeColumnsOnLoad, rowBorders, withBorder,
        columnsOverrides, selectedRows, setSelectedRows,
        enableAdd, enableDelete, enableEdit, enableView,
        actionsButtonVariant, showActions, entity,
        renderOnlyWhenVisible, gridHeight,
        executeViewState, dataGridAPI, viewState, formMode,
    } = useComponentDefaultProps('DataMapGrid', DataMapGridDefaultProps, props);

    const visibilityDivRef = useRef<HTMLDivElement>(null);
    const isFirstVisible = useFirstVisible(visibilityDivRef);

    const { columns, rows, isLoading } = dataGridAPI;

    return (
        <>
            {renderOnlyWhenVisible && isFirstVisible === false ?
                <div ref={visibilityDivRef} style={{ height: gridHeight }}></div>
                :
                <section>
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

                        parentFormMode={formMode}

                    />
                    <Box pos={"relative"} mt="sm" mb="sm" >
                        {(isLoading || executeViewState.loading) &&
                            <FakeProgressBar pos={"absolute"} style={{ top: 0, width: "100%" }} size="xs" />
                        }
                        {!isLoading && !executeViewState.loading && executeViewState.error &&
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
                            columnsOverrides={columnsOverrides}
                            selectedRows={selectedRows}
                            setSelectedRows={setSelectedRows}
                            timeZoneOffset={entity?.API.client.TIMEZONE_OFFSET}
                        />
                    </Box>
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
                </section>
            }
        </>
    );
}