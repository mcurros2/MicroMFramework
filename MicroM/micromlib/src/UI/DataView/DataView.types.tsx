import { MantineNumberSize, SelectItem } from "@mantine/core";
import { ComponentType, CSSProperties } from "react";
import { DataResult, OperationStatus, ValuesObject } from "../../client";
import { ActionIconVariant, ButtonVariant, EntityUILabels, FormMode, UseEntityUIProps } from "../Core";
import { DataGridToolbarSizes } from "../DataGrid";
import { EntityCardProps } from "../EntityCard/EntityCard";
import { DataViewCardContainerProps } from "./DataViewCardContainer";

export type DataViewLimit = '50' | '100' | '500' | '1000' | '10000' | '0';

export const DataViewLimitData = (rowsLabel: string, unlimitedLabel: string) => {
    return [
        { value: "50", label: `50 ${rowsLabel}` },
        { value: "100", label: `100 ${rowsLabel}` },
        { value: "500", label: `500 ${rowsLabel}` },
        { value: "1000", label: `1000 ${rowsLabel}` },
        { value: "10000", label: `10000 ${rowsLabel}` },
        { value: "0", label: unlimitedLabel },
    ] as SelectItem[]
}

export type DataViewSelection = number[];

export interface DataViewRecord<T extends ValuesObject> {
    keys: ValuesObject,
    data: T
}

export type DataViewSelectionChangedCallback = (selection: DataViewSelection, keys: ValuesObject[]) => void;
export type DataViewSelectionChangedHandler = (selection: DataViewSelection) => void;

export type DataViewSelectionMode = 'single' | 'multi';

export interface DataViewLabels extends EntityUILabels {
    rowsReturnedLabel: string,
    selectedRowsLabel: string,
    limitLabel: string,
    rowsLabel: string,
    unlimitedLabel: string,
    resultLimitLabel: string,
    loadMoreLabel: string,
    fetchingDataLabel: string,
    noRecordsFoundLabel: string,
}

export interface DataViewProps extends Omit<UseEntityUIProps, 'labels'> {
    viewName: string,
    search?: string[],
    limit?: DataViewLimit,
    refreshOnInit?: boolean,
    selectionMode: DataViewSelectionMode,
    preserveSelection?: boolean,
    autoFocus?: boolean,
    onDataRefresh?: (result: OperationStatus<DataResult[]>) => void,
    allwaysRefreshOnEntityClose?: boolean,

    // toolbar
    toolbarIconVariant?: ActionIconVariant,
    actionsButtonVariant?: ButtonVariant,
    toolbarSize?: DataGridToolbarSizes,
    enableAdd?: boolean,
    enableEdit?: boolean,
    enableDelete?: boolean,
    enableView?: boolean,
    enableExport?: boolean,
    filtersFormSize?: MantineNumberSize,
    setInitialFiltersFromColumns?: boolean,
    visibleFilters?: string[],

    showAppliedFilters?: boolean,
    showRefreshButton?: boolean,
    showFiltersButton?: boolean,
    showSearchInput?: boolean,
    showSelectRowsButton?: boolean,

    searchPlaceholder?: string,
    hideCheckboxToggle?: boolean,
    showActions?: boolean,
    showToolbar?: boolean,

    showDeleteOnlyWhenMultiselect?: boolean,

    // Cards

    CardContainer?: ComponentType<DataViewCardContainerProps>,
    CardRowAlign?: CSSProperties['alignItems'],
    cardHrefRootURL?: string
    cardHrefTarget?: string
    Card: ComponentType<EntityCardProps<ValuesObject>>,
    onSelectionChanged?: DataViewSelectionChangedCallback,
    notExportableColumns?: number[],
    labels?: DataViewLabels,
    itemsPerPage?: number,

    convertResultToLocaleString?: boolean
    formMode?: FormMode,


}

