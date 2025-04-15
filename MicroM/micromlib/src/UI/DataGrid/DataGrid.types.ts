import { MantineNumberSize } from "@mantine/core";
import { DataResult, OperationStatus, ValuesObject } from "../../client";
import { ActionIconVariant, ButtonVariant, EntityUILabels, FormMode, UseEntityUIProps } from "../Core";
import { DataViewLimit } from "../DataView/DataView.types";
import { GridColumnsOverrides, GridRecord, GridSelection, GridSelectionMode } from "../Grid";
import { DataGridToolbarSizes } from "./DataGridToolbar";

export interface DataGridStateProps {
    setRefresh: React.Dispatch<React.SetStateAction<boolean>>,
    setSearchText: React.Dispatch<React.SetStateAction<string[] | undefined>>,
    executeViewState: OperationStatus<DataResult[]>
}

export interface DataGridLabels extends EntityUILabels {
    rowsReturnedLabel: string,
    selectedRowsLabel: string,
    limitLabel: string,
    rowsLabel: string,
    unlimitedLabel: string,
}

export type DataGridSelectionChangedCallback = (selection: GridSelection, keys: DataGridSelectionKeys) => void

export interface DataGridProps extends UseEntityUIProps {
    viewName?: string,
    search?: string[],
    limit?: DataViewLimit,
    refreshOnInit?: boolean,
    selectionMode: GridSelectionMode,
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
    showActions?: boolean,
    setInitialFiltersFromColumns?: boolean,
    visibleFilters?: string[],

    // grid
    columnBorders?: boolean,
    rowBorders?: boolean,
    withBorder?: boolean,
    autoSizeColumnsOnLoad?: boolean,
    labels?: DataGridLabels,
    columnsOverrides?: GridColumnsOverrides,
    renderOnlyWhenVisible?: boolean,

    showToolbar?: boolean,
    showActionsToolbar?: boolean,
    enableImport?: boolean,

    gridHeight?: string | number | 'auto',
    autoSelectFirstRow?: boolean,
    onSelectionChanged?: DataGridSelectionChangedCallback,
    doubleClickAction?: 'edit' | 'view' | 'none' | ((record: GridRecord) => void),
    formMode?: FormMode,

    notExportableColumns?: number[],

    showColumnsConfigMenu?: boolean,
}

export type DataGridSelectionKeys = ValuesObject[]

