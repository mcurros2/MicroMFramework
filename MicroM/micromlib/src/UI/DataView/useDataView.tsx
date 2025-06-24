import { useCallback, useEffect, useRef, useState } from "react";
import { EntityClientAction, convertRecordToValuesObject, exportToExcel, toCamelCase } from "../../Entity";
import { DBStatusResult, DataResult, OperationStatus, SQLType, Value, ValuesObject, ValuesRecord } from "../../client";
import { useEntityUI, useLocaleFormat } from "../Core";
import { DataGridStateProps } from "../DataGrid/DataGrid.types";
import { DataViewProps, DataViewRecord, DataViewSelection, DataViewSelectionChangedHandler } from "./DataView.types";

export interface useDataViewReturnType {
    handleSelectionChanged: DataViewSelectionChangedHandler;
    handleDeleteRecord: (keys: ValuesObject, element?: HTMLElement) => Promise<void>;
    handleDeleteClick: (element?: HTMLElement) => Promise<void>;
    handleEditClick: (keys: ValuesObject, element?: HTMLElement) => Promise<void>;
    handleAddClick: (element?: HTMLElement) => Promise<void>;
    handleViewClick: (keys: ValuesObject, element?: HTMLElement) => Promise<void>;
    handleToggleSelectable: () => void;
    handleRefresh: (searchText: string[] | undefined) => void;
    handleExecuteAction: (action: EntityClientAction, recordIndex?: number, element?: HTMLElement) => Promise<boolean | undefined>;
    handleExport: () => void;
    selectedRowsCount: number;
    limitRows: string | null;
    setLimitRows: React.Dispatch<React.SetStateAction<string | null>>;
    showSelectCheckbox: boolean;
    isLoading: boolean;
    executeViewState: OperationStatus<DataResult[]>;
    data: DataViewRecord<ValuesObject>[];
    handleDeselectAllRecords: () => Promise<void>;
    handleDeselectRecord: (record_index: number) => Promise<void>;
    handleSelectRecord: (record_index: number) => Promise<void>;
    handleSelectAllRecords: () => Promise<void>;
    selectedRecords: DataViewSelection;
    toggleSelectable: boolean;
    handleLoadMore: () => void;
    displayedItemsCount: number;
    recordsCount?: number;
}


export function useDataView(props: DataViewProps, stateProps: DataGridStateProps): useDataViewReturnType {
    const {
        entity, parentKeys, viewName, limit, onSelectionChanged, modalFormSize, onModalSaved,
        labels, saveFormBeforeAdd, parentFormAPI, allwaysRefreshOnEntityClose, notExportableColumns, itemsPerPage, onActionExecuted,
        convertResultToLocaleString, withModalFullscreenButton,
        onDataRefresh
    } = props;

    const localeFormat = useLocaleFormat({timeZoneOffset: entity?.API.client.TIMEZONE_OFFSET || 0});

    const { setRefresh, setSearchText, executeViewState } = stateProps;

    const selectedRecords = useRef<DataViewSelection>([]);

    const [selectedRowsCount, setSelectedRowsCount] = useState(0);

    const [toggleSelectable, setToggleSelectable] = useState(false);

    const [limitRows, setLimitRows] = useState<string | null>(limit ? limit as string : null);
    const [isLoading, setIsLoading] = useState(false);


    const data = useRef<DataViewRecord<ValuesObject>[]>([]);

    const [displayedItemsCount, setDisplayedItemsCount] = useState(itemsPerPage!);

    const [viewResult, setViewResult] = useState<DataResult | null>(null);


    // MMC: we can't use callbacks here because we need to use the latest values of the state variables
    const internalRefresh = useCallback(() => {
        setRefresh((prev) => !prev);
    }, [setRefresh]);

    const handleRefresh = useCallback((search_text: string[] | undefined) => {
        setSearchText(search_text);
        setRefresh((prev) => !prev);
    }, [setRefresh, setSearchText]);

    const handleModalSaved = useCallback(async (new_status: OperationStatus<DBStatusResult | null>) => {
        if (!new_status.error && !new_status.loading && new_status.data && !allwaysRefreshOnEntityClose) internalRefresh();
        if (onModalSaved) await onModalSaved(new_status);
    }, [allwaysRefreshOnEntityClose, internalRefresh, onModalSaved]);

    const handleAllwaysRefreshOnClose = useCallback(() => {
        if (allwaysRefreshOnEntityClose) internalRefresh();
    }, [allwaysRefreshOnEntityClose, internalRefresh]);

    const UIAPI = useEntityUI({
        entity, parentKeys, modalFormSize, parentFormAPI, saveFormBeforeAdd, withModalFullscreenButton, onModalSaved: handleModalSaved,
        onModalClosed: handleAllwaysRefreshOnClose,
        onRecordsDeleted: internalRefresh, onActionRefreshOnClose: internalRefresh, labels, onActionExecuted
    });

    const getRecordKeys = useCallback((record: ValuesRecord): ValuesObject => {
        if (!entity || !record) return {}; // MMC: this should never happen

        const keys: ValuesObject = {};

        // MMC: keyMappings is an object with the column names as keys and the index of the column in the viewResult as value
        const key_mappings = entity.def.views[viewName].keyMappings;
        for (const columnName in key_mappings) {
            const idx = key_mappings[columnName]; // MMC: index of the column in the record
            if (idx >= record.length) continue;
            keys[columnName] = record[idx];
        }

        return keys;
    }, [entity, viewName]);

    const getSelectionKeys = useCallback(() => {
        // MMC: if viewResult is null, it means that the view has not been executed yet
        if (!data) return [];
        if (selectedRecords.current.length === 0) return [];

        const selection = selectedRecords.current;

        const result: ValuesObject[] = [];
        // MMC: dataViewSelection is an array of indexes of the selected records in the viewResult
        for (let i = 0; i < selection.length; i++) {
            const keys = data.current[selection[i]].keys;
            if (keys) result.push(keys);
        }
        return result;
    }, []);


    const handleSelectRecord = useCallback(async (record_index: number) => {
        if (selectedRecords.current.includes(record_index)) return;

        selectedRecords.current.push(record_index);
        setSelectedRowsCount(selectedRecords.current.length);
        if (onSelectionChanged) onSelectionChanged(selectedRecords.current, getSelectionKeys());

    }, [getSelectionKeys, onSelectionChanged]);

    const handleDeselectRecord = useCallback(async (record_index: number) => {
        const index_to_remove = selectedRecords.current.indexOf(record_index);
        if (index_to_remove === -1) return;

        selectedRecords.current.splice(index_to_remove, 1);

        setSelectedRowsCount(selectedRecords.current.length);
        if (onSelectionChanged) onSelectionChanged(selectedRecords.current, getSelectionKeys());
    }, [getSelectionKeys, onSelectionChanged]);

    const handleSelectAllRecords = useCallback(async () => {
        selectedRecords.current = data.current.map((_, index) => index) ?? [];
        setSelectedRowsCount(selectedRecords.current.length);
        if (onSelectionChanged) onSelectionChanged(selectedRecords.current, getSelectionKeys());
    }, [getSelectionKeys, onSelectionChanged]);

    const handleDeselectAllRecords = useCallback(async () => {
        selectedRecords.current = [];
        setSelectedRowsCount(selectedRecords.current.length);
        if (onSelectionChanged) onSelectionChanged(selectedRecords.current, getSelectionKeys());
    }, [getSelectionKeys, onSelectionChanged]);


    const handleSelectionChanged = useCallback<DataViewSelectionChangedHandler>(async (newSelection) => {
        selectedRecords.current = newSelection.filter(index => index >= 0 && index < selectedRecords.current.length);;
        setSelectedRowsCount(selectedRecords.current.length);
        if (onSelectionChanged) onSelectionChanged(selectedRecords.current, getSelectionKeys());
    }, [onSelectionChanged, getSelectionKeys]);

    const handleToggleSelectable = useCallback(() => {
        setToggleSelectable(prev => !prev);
    }, []);

    const handleEditClick = useCallback(async (keys: ValuesObject, element?: HTMLElement) => {
        if (!entity || entity.Form === null) return;
        if (!keys) return;

        await UIAPI.handleEditClick(keys, element);
    }, [UIAPI, entity]);

    const handleViewClick = useCallback(async (keys: ValuesObject, element?: HTMLElement) => {
        if (!entity || entity.Form === null) return;
        if (!keys) return;

        await UIAPI.handleViewClick(keys, element);
    }, [entity, UIAPI]);

    const handleDeleteClick = useCallback(async (element?: HTMLElement) => {
        const keys = getSelectionKeys();
        await UIAPI.handleDeleteClick(keys, element);
    }, [UIAPI, getSelectionKeys]);

    const handleDeleteRecord = useCallback(async (keys: ValuesObject, element?: HTMLElement) => {
        if (!keys) return;

        await UIAPI.handleDeleteRecord(keys, element);
    }, [UIAPI]);

    const handleExecuteAction = useCallback(async (action: EntityClientAction, recordIndex?: number, element?: HTMLElement) => {
        if (recordIndex !== undefined) {
            selectedRecords.current = [recordIndex];
        }
        const keys = getSelectionKeys();
        return await UIAPI.handleExecuteAction(action, keys, element);
    }, [UIAPI, getSelectionKeys]);

    const handleExport = useCallback(() => {
        if (!viewResult) return;
        exportToExcel([viewResult], notExportableColumns);
    }, [notExportableColumns, viewResult]);

    const formatValue = useCallback((value: Value, sqlType: SQLType) => {
        const rawValue = localeFormat.formatValue(localeFormat.getNativeValue(value, sqlType), sqlType);
        return (rawValue === 'null') ? '' : rawValue;
    }, [localeFormat]);

    const handleLoadMore = () => {
        setDisplayedItemsCount(currentCount => Math.min(currentCount! + itemsPerPage!, data.current.length));
    };

    useEffect(() => {
        if (executeViewState.loading) {
            setIsLoading(true);
            setDisplayedItemsCount(itemsPerPage!);
        } else if (executeViewState.error) {
            setIsLoading(false);
        } else if (!executeViewState.error && executeViewState.data?.length) {
            const data_result = executeViewState.data[0];
            const headers = data_result.Header.map(toCamelCase); // transform header names

            const new_data = data_result.records.map((record) => {
                return {
                    keys: getRecordKeys(record),
                    data: convertRecordToValuesObject(record, headers, data_result.typeInfo, convertResultToLocaleString ? formatValue : undefined)
                };
            });
            data.current = new_data;

            setViewResult(data_result);
            setIsLoading(false);
            if (onDataRefresh) onDataRefresh(executeViewState);
        } else if (!executeViewState.error && executeViewState.data?.length === 0) {
            data.current = [];

            setViewResult(null);
            setIsLoading(false);
        }
    }, [executeViewState, formatValue, getRecordKeys, convertResultToLocaleString, onDataRefresh]);

    return {
        handleSelectionChanged,
        handleDeleteRecord,
        handleDeleteClick,
        handleEditClick,
        handleAddClick: UIAPI.handleAddClick,
        handleViewClick,
        handleToggleSelectable,
        handleRefresh,
        handleExecuteAction,
        handleExport,
        selectedRowsCount,
        limitRows,
        setLimitRows,
        showSelectCheckbox: toggleSelectable,
        isLoading,
        executeViewState,
        data: data.current.slice(0, displayedItemsCount),
        handleDeselectAllRecords,
        handleDeselectRecord,
        handleSelectRecord,
        handleSelectAllRecords,
        selectedRecords: selectedRecords.current,
        toggleSelectable,
        handleLoadMore,
        displayedItemsCount,
        recordsCount: data.current.length,
    }
}
