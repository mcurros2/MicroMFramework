import { Button, Group, Loader, SelectItem, Space, Stack, Text, useComponentDefaultProps } from "@mantine/core";
import { ForwardedRef, forwardRef, useState } from "react";
import { AlertError, useExecuteView, useViewState } from "../Core";
import { DataGridToolbar } from "../DataGrid";
import { DataGridActionsToolbar } from "../DataGrid/DataGridActionsToolbar";
import { DataViewProps } from "./DataView.types";
import { DataViewCardContainer } from "./DataViewCardContainer";
import { useDataView } from "./useDataView";


export const DataViewDefaultProps: Partial<DataViewProps> = {
    search: [],
    limit: "1000",
    toolbarIconVariant: "light",
    actionsButtonVariant: "light",
    modalFormSize: "lg",
    enableAdd: true,
    enableEdit: true,
    enableDelete: true,
    enableExport: true,
    CardContainer: DataViewCardContainer,
    itemsPerPage: 50,
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
        resultLimitLabel: "Results are limited to %s records.",
        loadMoreLabel: "Load More",
        fetchingDataLabel: "Fetching data...",
        noRecordsFoundLabel: "No records found",
        YouMustSelectAtLeast: "You must select at least",
        YouMustSelectBetween: "You must select between",
        YouMustSelect: "You must select",
        YouMustSelectMaximum: "You must select a maximum of",
    },
    filtersFormSize: "md",
    convertResultToLocaleString: true,
    showActions: true,
    showToolbar: true,
    showDeleteOnlyWhenMultiselect: true,
    withModalFullscreenButton: true,
    CardRowAlign: "flex-start",
    RowsContainer: Group,
}

export const DataView = forwardRef(function DataView(props: DataViewProps, ref: ForwardedRef<HTMLElement> | undefined) {
    props = useComponentDefaultProps('DataView', DataViewDefaultProps, props);
    const {
        entity, autoFocus, toolbarIconVariant,
        actionsButtonVariant, enableAdd, enableEdit, enableDelete, enableView, enableExport, labels,
        Card, CardContainer, limit, search, viewName, filtersFormSize, toolbarSize,
        showAppliedFilters, showRefreshButton, hideCheckboxToggle, showFiltersButton, searchPlaceholder,
        showActions, parentKeys, visibleFilters, setInitialFiltersFromColumns, cardHrefRootURL, cardHrefTarget,
        showSearchInput, showSelectRowsButton, showToolbar, showDeleteOnlyWhenMultiselect, parentFormAPI, formMode,
        CardRowAlign, RowsContainer
    } = props;

    const [searchData, setSearchData] = useState<SelectItem[]>(search?.map(s => { return { value: s, label: s } }) as SelectItem[]);

    const viewState = useViewState(search, limit);

    const executeViewState = useExecuteView(entity, parentKeys, viewName, viewState.searchText, viewState.limitRows, viewState.refresh, viewState.filterValues);

    const dataViewAPI = useDataView(props, { executeViewState, setRefresh: viewState.setRefresh, setSearchText: viewState.setSearchText });
    const { handleLoadMore } = dataViewAPI;

    const limit_number = parseInt(limit || '0');

    const effectiveFormMode = formMode || parentFormAPI?.formMode || 'add';

    if (entity?.def.views[viewName] === undefined) {
        console.warn(`DataView: View ${viewName} not found in entity ${entity?.def.name}`);
    }

    return (
        <section ref={ref}>
            <Stack spacing="sm">
                {showToolbar &&
                    <>
                        <DataGridToolbar
                            {...labels!}

                            enableExport={enableExport}
                            onExportClick={dataViewAPI.handleExport}

                            onRefreshClick={dataViewAPI.handleRefresh}
                            onCheckboxToggle={dataViewAPI.handleToggleSelectable}
                            autoFocus={autoFocus}
                            toolbarIconVariant={toolbarIconVariant}

                            searchText={viewState.searchText}
                            setSearchText={viewState.setSearchText}
                            searchData={searchData}
                            setSearchData={setSearchData}

                            client={entity?.API.client}

                            FiltersEntity={(entity && viewName && entity?.def.views[viewName]) ? entity?.def.views[viewName].FiltersEntity : undefined}

                            filterValues={viewState.filterValues}
                            setFilterValues={viewState.setFilterValues}

                            filtersDescription={viewState.filtersDescription}
                            setFiltersDescription={viewState.setFiltersDescription}

                            filtersFormSize={filtersFormSize!}

                            size={toolbarSize}

                            showAppliedFilters={showAppliedFilters}
                            showRefreshButton={showRefreshButton}
                            showFiltersButton={showFiltersButton}
                            showSearchInput={showSearchInput}
                            showSelectRowsButton={showSelectRowsButton}


                            hideCheckboxToggle={hideCheckboxToggle}
                            searchPlaceholder={searchPlaceholder}

                            visibleFilters={visibleFilters}
                            initialColumnFilters={setInitialFiltersFromColumns && entity ? entity.def.columns : undefined}
                        />
                    </>
                }
                {showActions &&
                    <DataGridActionsToolbar
                        {...labels!}

                        viewName={viewName}

                        onAddClick={dataViewAPI.handleAddClick}
                        onDeleteClick={dataViewAPI.handleDeleteClick}

                        enableAdd={enableAdd}
                        enableEdit={false}
                        enableDelete={enableDelete && (showDeleteOnlyWhenMultiselect ? dataViewAPI.showSelectCheckbox : true)}
                        enableView={false}

                        actionsButtonVariant={actionsButtonVariant}
                        clientActions={entity ? entity.def.clientActions : {}}

                        handleExecuteAction={dataViewAPI.handleExecuteAction}

                        parentFormMode={effectiveFormMode}
                    />
                }
                {!showActions &&
                    <Space h="xs" />
                }
                {dataViewAPI.isLoading &&
                    <Group>
                        <Loader size="sm" />
                        <Text fz="sm" c="dimmed">{labels?.fetchingDataLabel}</Text>
                    </Group>
                }
                {!dataViewAPI.isLoading && dataViewAPI.executeViewState.error &&
                    <AlertError mb="xs">Error: {dataViewAPI.executeViewState.error.status} {dataViewAPI.executeViewState.error.message} {dataViewAPI.executeViewState.error.statusMessage}</AlertError>
                }
                {!dataViewAPI.isLoading && !dataViewAPI.executeViewState.error && dataViewAPI.data.length > 0 && dataViewAPI.recordsCount === limit_number && limit_number !== 0 &&
                    <Text fz="sm" align="center" fw={500} c="dimmed">{labels?.resultLimitLabel?.replace('%s', limit || '')}</Text>
                }
                {!dataViewAPI.isLoading && !dataViewAPI.executeViewState.error && dataViewAPI.data.length === 0 && search &&
                    <Text fz="sm" align="center" fw={500} c="dimmed">{labels?.noRecordsFoundLabel}</Text>
                }
                {dataViewAPI.data.length > 0 && RowsContainer &&
                    <RowsContainer {...(CardRowAlign ? { align: CardRowAlign } : null)}>
                        {
                            CardContainer && entity && dataViewAPI.data.map((record, index) => {
                                return <CardContainer
                                    key={`CC_${entity.def.name}-${index}`}
                                    record={record}
                                    EntityCard={Card}
                                    recordIndex={index}
                                    entity={entity}
                                    enableDelete={effectiveFormMode !== 'view' && enableDelete}
                                    enableEdit={effectiveFormMode !== 'view' && enableEdit}
                                    enableView={enableView !== undefined ? enableView : effectiveFormMode === 'view'}
                                    handleSelectRecord={dataViewAPI.handleSelectRecord}
                                    handleDeselectRecord={dataViewAPI.handleDeselectRecord}
                                    handleDeleteClick={dataViewAPI.handleDeleteRecord}
                                    handleEditClick={dataViewAPI.handleEditClick}
                                    handleViewClick={dataViewAPI.handleViewClick}
                                    handleExecuteAction={dataViewAPI.handleExecuteAction}
                                    selected={dataViewAPI.selectedRecords.includes(index)}
                                    toggleSelectable={dataViewAPI.toggleSelectable}
                                    refreshView={() => viewState.setRefresh((prev) => !prev)}
                                    cardHrefRootURL={cardHrefRootURL}
                                    cardHrefTarget={cardHrefTarget}
                                />
                            })
                        }
                    </RowsContainer>
                }
                {!dataViewAPI.isLoading && !dataViewAPI.executeViewState.error && dataViewAPI.data.length > 0 && (dataViewAPI.recordsCount || 0) > dataViewAPI.displayedItemsCount &&
                    <Button size="xs" variant="light" onClick={handleLoadMore}>{labels?.loadMoreLabel}</Button>
                }
            </Stack>
        </section>
    )
});
