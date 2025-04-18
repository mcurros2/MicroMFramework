import ExcelJS from 'exceljs';
import { useCallback, useEffect, useRef, useState } from "react";
import { EntityClientAction } from "../../Entity";
import { DBStatusResult, OperationStatus, Value, ValuesObject, ValuesRecord } from "../../client";
import { useEntityUI } from "../Core";
import { GridColumn, GridDoubleClickCallback, GridSelection, GridSelectionChangedCallback } from "../Grid";
import { DataGridProps, DataGridSelectionKeys, DataGridStateProps } from "./DataGrid.types";

export function useDataGrid(props: DataGridProps, stateProps: DataGridStateProps) {
    const {
        entity, parentKeys, viewName, onSelectionChanged, modalFormSize,
        labels, saveFormBeforeAdd, parentFormAPI, allwaysRefreshOnEntityClose, onAddClick, onModalSaved,
        onDataRefresh, onActionExecuted, formMode, doubleClickAction, notExportableColumns, withModalFullscreenButton,
        initialHiddenColumns
    } = props;

    const { setRefresh, setSearchText, executeViewState } = stateProps;

    const selection = useRef<GridSelection>([]);
    const selectionKeys = useRef<DataGridSelectionKeys>([]);

    const [selectedRowsCount, setSelectedRowsCount] = useState(0);

    const [toggleSelectable, setToggleSelectable] = useState(false);

    const [isLoading, setIsLoading] = useState(false);

    const [columns, setColumns] = useState<GridColumn[]>();
    const [rows, setRows] = useState<ValuesRecord[]>();

    const internalRefresh = useCallback(() => {
        setRefresh((prev) => !prev);
    }, [setRefresh]);

    const handleRefresh = useCallback((search_text: string[] | undefined) => {
        setSearchText(search_text);
        setRefresh((prev) => !prev);
    }, [setRefresh, setSearchText]);

    const handleModalSaved = useCallback(async (new_status: OperationStatus<DBStatusResult | null>) => {
        if (!new_status.error && !new_status.loading && new_status.data && !handleAlwaysRefreshOnClose) internalRefresh();
        if (onModalSaved) await onModalSaved(new_status);
    }, [internalRefresh, onModalSaved]);

    const handleAlwaysRefreshOnClose = useCallback(async () => {
        if (allwaysRefreshOnEntityClose) await internalRefresh();
    }, [allwaysRefreshOnEntityClose, internalRefresh]);

    const UIAPI = useEntityUI({
        entity, parentKeys, modalFormSize, parentFormAPI, saveFormBeforeAdd, onModalSaved: handleModalSaved, onModalClosed: handleAlwaysRefreshOnClose,
        onRecordsDeleted: internalRefresh, onActionRefreshOnClose: internalRefresh, labels, onAddClick, onActionExecuted, withModalFullscreenButton
    });


    const getSelectionKeys = useCallback((selection: GridSelection) => {
        const result: DataGridSelectionKeys = [];
        if (entity && viewName) {
            for (let i = 0; i < selection.length; i++) {
                const record = selection[i];
                const keys: ValuesObject = {};
                const record_properties = Object.keys(record);
                const key_mappings = entity.def.views[viewName].keyMappings;
                for (const columnName in key_mappings) {
                    const idx = key_mappings[columnName];
                    if (idx >= record_properties.length) continue;
                    keys[columnName] = record[record_properties[idx]] as Value;
                }
                if (keys) result.push(keys);
            }
        }
        return result;
    }, [entity, viewName]);


    const handleToggleSelectable = useCallback(() => {
        setToggleSelectable(prev => !prev);
    }, []);

    const handleSelectionChanged = useCallback<GridSelectionChangedCallback>(async (newSelection) => {
        selection.current = newSelection;
        selectionKeys.current = getSelectionKeys(newSelection);
        setSelectedRowsCount(newSelection.length);
        if (onSelectionChanged) onSelectionChanged(newSelection, selectionKeys.current);
    }, [onSelectionChanged, getSelectionKeys]);


    const handleEditClick = useCallback(async (element?: HTMLElement) => {
        if (entity?.Form === null) return;
        const keys = selectionKeys.current;
        if (keys.length) {
            await UIAPI.handleEditClick(keys[0], element);
        }
    }, [entity?.Form, UIAPI]);

    const handleViewClick = useCallback(async (element?: HTMLElement) => {
        if (entity?.Form === null) return;
        const keys = selectionKeys.current;
        if (keys.length) {
            await UIAPI.handleViewClick(keys[0], element);
        }
    }, [entity?.Form, UIAPI]);

    const handleDeleteClick = useCallback(async (element?: HTMLElement) => {
        const keys = selectionKeys.current;
        await UIAPI.handleDeleteClick(keys, element);
    }, [UIAPI]);

    const handleExecuteAction = useCallback(async (action: EntityClientAction, recordIndex?: number, element?: HTMLElement) => {
        const keys = selectionKeys.current;
        await UIAPI.handleExecuteAction(action, keys, element);
    }, [UIAPI]);

    const handleDoubleClick = useCallback<GridDoubleClickCallback>(async (record) => {
        if (doubleClickAction === 'none' || doubleClickAction === undefined) return;

        const form_mode = formMode || parentFormAPI?.formMode;
        if (!form_mode) {
            console.warn('DataGrid double click: Form mode not defined or parent form api not set ');
            return
        }

        if (doubleClickAction === 'edit') {
            if (form_mode === 'edit') {
                await handleEditClick();
            }
            if (form_mode === 'view') {
                await handleViewClick();
            }
        } else if (doubleClickAction === 'view') {
            if (form_mode === 'view') await handleViewClick();
        } else {
            await doubleClickAction(record);
        }

        await handleEditClick();
    }, [doubleClickAction, formMode, handleEditClick, handleViewClick, parentFormAPI?.formMode]);


    const handleImportDataClick = useCallback(async () => {
        await UIAPI.handleImportDataClick();
    }, [UIAPI]);


    // MMC: this is special here for the Grid component as it uses columns and records
    const handleExport = useCallback(async () => {

        if (!rows?.length || !columns?.length) return;

        const currentDate = new Date();
        const filename = `export-${currentDate.getFullYear()}${(currentDate.getDate() + '').padStart(2, '0')}${(currentDate.getMonth() + 1 + '').padStart(2, '0')}_${(currentDate.getHours() + '').padStart(2, '0')}${(currentDate.getMinutes() + '').padStart(2, '0')}${(currentDate.getSeconds() + '').padStart(2, '0')}.xlsx`;

        const workbook = new ExcelJS.Workbook();
        const worksheet = workbook.addWorksheet('Data');

        // filter columns to exclude not exportable columns in notExportableColumns
        const exportableColumns = columns.filter((col, index) => {
            return !notExportableColumns?.includes(index);
        });

        // headers
        worksheet.addRow(exportableColumns.map(col => col.text));

        // Rows
        for (let r = 0; r < rows.length; r++) {
            const excel_row: Value[] = [];
            for (let c = 0; c < columns.length; c++) {
                if (notExportableColumns?.includes(c)) continue;

                excel_row[c] = rows[r][c];
            }
            worksheet.addRow(excel_row);
        }

        try {
            if (window.Blob && window.URL) {
                const buffer = await workbook.xlsx.writeBuffer();
                const blob = new Blob([buffer], {
                    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
                });

                const link = document.createElement('a');
                link.href = URL.createObjectURL(blob);
                link.download = filename;
                link.click();
                URL.revokeObjectURL(link.href);
            }
        } catch (ex) {
            console.error("Your browser does not support exporting data.");
        }
    }, [columns, notExportableColumns, rows]);

    useEffect(() => {
        if (executeViewState.loading) {
            setIsLoading(true);
        } else if (executeViewState.error) {
            setIsLoading(false);
        } else if (!executeViewState.error && executeViewState.data?.length) {
            setColumns(executeViewState.data[0].Header.map((columnText, index) => ({
                field: index.toString(),
                text: columnText,
                sqlType: executeViewState.data![0].typeInfo[index],
                hidden: initialHiddenColumns ? initialHiddenColumns.includes(index) : false,
            })));
            setRows(executeViewState.data[0].records);
            setIsLoading(false);
            if (onDataRefresh) onDataRefresh(executeViewState);
        }
    }, [executeViewState.data, executeViewState.error, executeViewState.loading, onDataRefresh, initialHiddenColumns]);


    return {
        handleSelectionChanged,
        handleDoubleClick,
        handleDeleteClick,
        handleEditClick,
        handleAddClick: UIAPI.handleAddClick,
        handleViewClick,
        handleToggleSelectable,
        handleRefresh,
        handleExecuteAction,
        handleExport,
        selectedRowsCount,
        showSelectCheckbox: toggleSelectable,
        UIAPI,
        columns,
        rows,
        isLoading,
        handleImportDataClick,
        setColumns,
    }
}
