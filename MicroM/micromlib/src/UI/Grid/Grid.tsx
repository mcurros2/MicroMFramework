import { Paper, useComponentDefaultProps } from "@mantine/core";
import { ForwardedRef, ReactNode, forwardRef, useImperativeHandle } from "react";
import "../../../libs/w2ui/w2ui.css";
import "./W2Grid.css";
import { useGrid } from "./useGrid";
import { w2columnField, w2record, w2recordId } from "./W2Grid";
import { SQLType } from "client";

export type GridSelectionMode = 'multi' | 'single';
export type GridColumnFormat = 'url'
    | 'email'
    | 'string'
    | 'check'
    | 'image'
    | 'html' //no format, allow html/scripts
//TODO: add auto detect

export type GridColumnRender = (value: unknown, cellElement?: HTMLElement, record?: GridRecord, field?: w2columnField) => ReactNode

export type GridRecord = w2record

export type GridSourceRecord = Record<string | number, unknown> | unknown[]

export type GridRecordId = w2recordId

export type GridColumn = {
    field: string,
    text: string,
    hidden?: boolean,
    format?: GridColumnFormat,
    autoSizeMax?: number,
    render?: GridColumnRender,
    sqlType?: SQLType,
}

export type GridColumnsOverrides = Record<string | number, Partial<GridColumn>>

export type GridSelection = GridRecord[]

export type GridDoubleClickCallback = (record: GridRecord) => void
export type GridSelectionChangedCallback = (selection: GridSelection) => void

export type GridOptions = {
    columns?: GridColumn[],
    rows?: GridSourceRecord[],
    gridHeight?: string | number | 'auto',
    selectionMode?: GridSelectionMode,
    onDoubleClick?: GridDoubleClickCallback,
    preserveSelection?: boolean,
    autoSelectFirstRow?: boolean,
    onSelectionChanged?: GridSelectionChangedCallback,
    showSelectCheckbox?: boolean,
    rowHeight?: number,
    stripped?: boolean,
    highlightOnHover?: boolean,
    columnBorders?: boolean,
    rowBorders?: boolean,
    withBorder?: boolean,
    autoSizeColumnsOnLoad?: boolean,
    columnsOverrides?: GridColumnsOverrides,
    selectedRows?: GridSelection,
    setSelectedRows?: (rows: GridSelection) => void,
    timeZoneOffset?: number,
}

export type GridImperative = {
    //refresh: () => void,
    //getSelectedRows: () => GridRecordId[],
    //getRecord: (record_id: GridRecordId) => GridRecord|null,
}

export const DefaultGridProps: Partial<GridOptions> = {
    gridHeight: "50vh",
    selectionMode: "multi",
    autoSelectFirstRow: true,
    preserveSelection: true,
    showSelectCheckbox: false,
    rowHeight: undefined, //auto adjusted when creating the grid unless set here
    stripped: true,
    highlightOnHover: true,
    columnBorders: true,
    rowBorders: false,
    withBorder: true,
    autoSizeColumnsOnLoad: true,
}

export const DefaultGridColumnProps: Partial<GridColumn> = {
    hidden: false,
    format: "string",
    autoSizeMax: 20 * parseFloat(getComputedStyle(document.documentElement).fontSize), //rem to px
}

export const Grid = forwardRef(function Grid(props: GridOptions, ref: ForwardedRef<GridImperative>) {
    props = useComponentDefaultProps('Grid', DefaultGridProps, props);

    const gridAPI = useGrid(props);

    useImperativeHandle(ref, () => gridAPI.imperative, [gridAPI.imperative]);

    return (
        <Paper style={{ borderRadius: "unset" }}>
            <div ref={gridAPI.boxRef} style={{ height: props.gridHeight }}></div>
            {gridAPI.cellsPortals}
        </Paper>
    );
})
