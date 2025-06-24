import { useMantineColorScheme } from "@mantine/core";
import { useEffect, useId, useMemo, useRef } from "react";
import { DefaultGridColumnProps, DefaultGridProps, GridColumn, GridImperative, GridOptions } from "../Grid";
import { w2column, w2columnAutoResizeEvent, w2event, w2grid, w2record } from "./W2Grid";
import { useW2ColumnRender } from "./useW2ColumnRender";

export function useGrid({
    columns, rows, selectionMode, onDoubleClick, preserveSelection, onSelectionChanged, showSelectCheckbox,
    rowHeight, stripped, highlightOnHover, columnBorders, rowBorders, withBorder, autoSizeColumnsOnLoad, autoSelectFirstRow,
    columnsOverrides, selectedRows, setSelectedRows, timeZoneOffset
}: GridOptions) {
    const gridRef = useRef<w2grid>(undefined!);
    const boxRef = useRef<HTMLDivElement>(null); //TODO: support different elements
    const gridName = useId().replace(/:/g, '_'); //fix conflict with ":" in CSS
    const preservedSelection = useRef<number[] | null>(null);
    const { colorScheme } = useMantineColorScheme();
    const columnAutoResizeSource = useRef<"user" | "system">("user"); //flag to determine if the autoResize triggered on the column is due to a user or system action, since w2ui has not considered it.

    const {
        w2columnRender, cellsPortals, clearCellPortals, isFirstVisible, performAutosize
    } = useW2ColumnRender({ columns, rows, columnsOverrides, columnAutoResizeSource, gridRef, autoSizeColumnsOnLoad, boxRef, timeZoneOffset });

    //if (isFirstVisible) console.log('grid is visible');

    useEffect(() => {
        //console.log("useGrid effect, gridName, new ref", gridName, showSelectCheckbox);
        gridRef.current = new w2grid({
            name: gridName,
            box: boxRef.current,
            reorderColumns: true,
            show: {
                columnMenu: false,
                //initial values
                selectColumn: showSelectCheckbox,
            },
            onDelete: function (event: w2event) {
                event.preventDefault();
            },
            onRefresh: function () {
                //console.debug("Grid refresh.");
            },
            onColumnAutoResize: async function (event: w2columnAutoResizeEvent) {
                const source = columnAutoResizeSource.current; //save source

                //console.debug("onColumnAutoResize", source, event.detail);

                //when doing autosize, the user's preference is taken into account if the user has manually set a size for the column, in which case the column cannot be made smaller but it can grow (keeping the preference but updating it to the new size so as not to complicate the logic)
                //!: There should always be a size set here
                if (source === "system" && event.detail.column._microm_userSize_cache && Math.min(Math.abs(event.detail.maxWidth), event.detail.column._microm_autoSizeMax || Infinity, event.detail.column.max || Infinity) < parseInt(event.detail.column.size!)) {
                    event.preventDefault();
                    return;
                }

                //we simulate a maximum autoSize limit since w2ui does not contemplate it
                const w2max_original = event.detail.column.max;
                if (event.detail.column._microm_autoSizeMax) {
                    // we check if it is the last column and in that case we try to fill the last space
                    const columns = gridRef.current.columns;
                    if (columns[columns.length - 1].field === event.detail.column.field) {
                        if (boxRef.current && boxRef.current.offsetWidth > 0) {
                            let columnsWidth: number | undefined = undefined;

                            // we take the container of the rows to be able to calculate the width of the last column, it can have vertical scroll
                            const container = boxRef.current.querySelector('.w2ui-grid-records') as HTMLDivElement | null;

                            if (container !== null) {
                                // we take the width of all columns except the last one
                                columnsWidth = container.clientWidth - columns.slice(0, -1).reduce((acc, col) => acc + (col.sizeCalculated ? parseInt(col.sizeCalculated!.replace(/\D/g, '')) : 0), 0);
                                columnsWidth = (event.detail.column.min && columnsWidth < event.detail.column.min) ? event.detail.column.min : columnsWidth;

                                // here we subtract 1 px since sizeCalculated is rounded up
                                event.detail.column.max = (columnsWidth && columnsWidth > 0) ? columnsWidth - (columns.length - 1) : undefined;
                            }
                        }
                        else {
                            event.detail.column.max = undefined;
                        }
                    }
                    else {
                        event.detail.column.max = event.detail.column._microm_autoSizeMax;
                    }
                }

                await event.complete;

                event.detail.column._microm_autoSize_cache = true;
                if (source === "user") event.detail.column._microm_userSize_cache = true;

                event.detail.column.max = w2max_original;
            },
            onColumnResize: async function (event: w2event) {
                await event.complete;
                //console.debug("column resize.", event.detail);
                gridRef.current.columns[(event.detail.column as number)]._microm_autoSize_cache = false;
                gridRef.current.columns[(event.detail.column as number)]._microm_userSize_cache = true;
            },
            onResize: async function (event: w2event) {
                await event.complete;
                setTimeout(() => gridRef.current.resizeRecords()); //workaround to the problem of using the grid inside "Tabs" components, in some cases it does not paint the empty rows (with 2 columns the problem appears, with 3 or more it does not)
            }
            //what follows is set only initially, changes are then handled with useEffect, this is to avoid unnecessary calls to refresh().
            //recordHeight: rowHeight, //CANNOT do this as we need w2ui to initialize with its default value so we can save it
        });

        if (!DefaultGridProps.rowHeight) DefaultGridProps.rowHeight = gridRef.current.recordHeight; //we get the default from w2ui (only if a default has not been specified)

        return () => {
            gridRef.current.destroy();
        }
    }, [gridName]); //do not add dependencies, use another useEffect


    useEffect(() => {
        gridRef.current.onDblClick = async (event: w2event) => {
            await event.complete;
            if (onDoubleClick) onDoubleClick(gridRef.current.getSelection()[0]);
        };
    }, [onDoubleClick]);

    // Managed selection state
    const selectionOrderRef = useRef<Map<number, number>>(new Map());
    const isInternalUpdate = useRef(false);

    // To keep selection order, w2ui makes an itnernal sort and breaks it
    useEffect(() => {
        selectionOrderRef.current.clear();

        selectedRows?.forEach((row, index) => {
            selectionOrderRef.current.set(row.recid, index);
        });
    }, [selectedRows]);

    useEffect(() => {
        gridRef.current.onSelect = async (event: w2event) => {
            await event.complete;
            if (onSelectionChanged || setSelectedRows) {
                const gridSelection = gridRef.current.getSelection(true);
                const unorderedSelection = gridSelection.map(selectedIndex => gridRef.current.records[selectedIndex]);

                // keep order
                const orderedSelection = unorderedSelection.sort((a, b) =>
                    (selectionOrderRef.current.get(a.recid) || 0) - (selectionOrderRef.current.get(b.recid) || 0)
                );

                if (setSelectedRows) {
                    isInternalUpdate.current = true;
                    setSelectedRows(orderedSelection);
                }
                if (onSelectionChanged) onSelectionChanged(orderedSelection);
            }
        };
    }, [onSelectionChanged, setSelectedRows]);

    useEffect(() => {
        if (!isInternalUpdate.current) {
            if (selectedRows && selectedRows.length > 0) {
                gridRef.current.selectNone();

                const selectedRecids = selectedRows.map(selectedRow => selectedRow.recid);
                gridRef.current.select(selectedRecids);
            }
            else {
                gridRef.current.selectNone();
            }
        }
        else {
            isInternalUpdate.current = false;
        }

    }, [selectedRows]);
    //

    useEffect(() => {
        switch (selectionMode!) {
            case "multi":
                gridRef.current.multiSelect = true;
                break;
            case "single":
                if (gridRef.current.multiSelect !== false) {
                    gridRef.current.multiSelect = false;

                    const sel = gridRef.current.getSelection();
                    if (sel.length > 1) {
                        gridRef.current.select(sel[0]);
                    }
                }
                break;
        }
    }, [selectionMode]);

    useEffect(() => {
        if (gridRef.current.show.selectColumn !== showSelectCheckbox) {
            gridRef.current.show.selectColumn = showSelectCheckbox!;
            gridRef.current.refresh();
        }
    }, [showSelectCheckbox]);

    useEffect(() => {
        if (rowHeight === undefined) return;
        //console.log("useGrid effect, rowHeight", rowHeight);
        if (gridRef.current.recordHeight != rowHeight) {
            gridRef.current.recordHeight = rowHeight;
            gridRef.current.refresh();
        }
    }, [rowHeight]);

    // MMC: save selection in preservedSelection
    useEffect(() => {
        //console.log("useGrid effect, preserveSelection", preserveSelection);
        if (preserveSelection) {
            if (gridRef.current.records.length) {
                preservedSelection.current = gridRef.current.getSelection();
            }
        } else {
            preservedSelection.current = null;
        }
    }, [rows, preserveSelection]);

    // MMC: columns
    useEffect(() => {
        //console.log("useGrid effect, columns, columnsOverrides", columns, columnsOverrides);
        //the idea of this is to do an efficient merge when there are changes in columns, since it was inefficient to remove and recreate the columns for each change

        if (!columns?.length) {
            if (gridRef.current.columns.length) {
                gridRef.current.removeColumn(...gridRef.current.columns.map(c => c.field)); //calls refresh()
            }
            return;
        }

        const w2colsUpdated = [];
        const w2colsAdded = [];
        const w2colsRemoved = [];

        for (let colIndex = 0; colIndex < columns.length; colIndex++) {
            const column = columns[colIndex];
            let w2column = gridRef.current.getColumn(column.field);

            if (w2column) {
                let updated = false;
                if (applyW2ColumnProps(w2column, DefaultGridColumnProps, column, columnsOverrides?.[column.field])) {
                    updated = true;
                    w2column._microm_autoSize_cache = false;
                }
                w2column.render = w2columnRender;
                if (updated) {
                    w2colsUpdated.push(w2column);
                }
            } else {
                w2column = {
                    sortable: true,
                    autoResize: true,
                    size: "0", //required by autosize
                    render: w2columnRender,
                    //workaround to the problem of column autosize, for some reason it does not affect the last columns in some cases and they remain at the minimum size, with this at least they look a little better.
                    min: 5 * parseFloat(getComputedStyle(document.documentElement).fontSize), //rem to px
                };
                applyW2ColumnProps(w2column, DefaultGridColumnProps, column, columnsOverrides?.[column.field]);
                w2colsAdded.push({
                    before: Math.min(colIndex + 1, columns.length), //keep in mind that the columns in w2grid may have been reordered, we insert the new columns at the index where they would be without taking into account those that were reordered
                    w2column: w2column
                });
            }
        }
        for (let w2colIndex = 0; w2colIndex < gridRef.current.columns.length; w2colIndex++) {
            const w2column = gridRef.current.columns[w2colIndex];
            const column = columns.find(c => c.field === w2column.field);
            if (!column) {
                w2colsRemoved.push(w2column);
            }
        }


        //avoid refresh until doing the whole merge
        const _w2refresh = gridRef.current.refresh;
        gridRef.current.refresh = () => undefined;

        try {

            if (w2colsRemoved.length) {
                gridRef.current.removeColumn(...w2colsRemoved.map(c => c.field)); //calls refresh()
            }

            if (w2colsAdded.length) {
                for (let i = 0; i < w2colsAdded.length; i++) {
                    const w2colToAdd = w2colsAdded[i];
                    gridRef.current.addColumn(w2colToAdd.w2column, w2colToAdd.before); //do not assign to .columns directly! as that skips important logic. calls refresh
                }
            }

        } finally {
            gridRef.current.refresh = _w2refresh;
        }

        gridRef.current.refresh();
        //console.debug("useGrid effect columns updated, added, removed", w2colsUpdated.length, w2colsAdded.length, w2colsRemoved.length);

    }, [columns, columnsOverrides]); // MMC: intentionally leaves out the render function dependency

    //the idea of this is to apply values to the properties of a w2ui column by merging GridColumn and overrides: w2column(mutates) <- column <- columnOverrides
    function applyW2ColumnProps(w2column: w2column, ...columns: (Partial<GridColumn> | undefined)[]): boolean {
        let effected = false;
        function _apply(w2prop: keyof w2column, prop: keyof GridColumn) {
            const decidedValueSource = columns.findLast(c => c && Object.prototype.hasOwnProperty.call(c, prop));
            if (decidedValueSource) {
                if (w2column[w2prop] !== decidedValueSource[prop]) {
                    (w2column as Record<string, unknown>/*ignore warn readonly*/)[w2prop] = decidedValueSource[prop];
                    effected = true;
                }
            }
        }

        _apply("field", "field");
        _apply("text", "text");
        _apply("hidden", "hidden"); //in case of undefined, the default of w2ui is used

        //these properties do not exist in w2ui, but we use them for change detection
        _apply("_microm_format", "format");
        _apply("_microm_render", "render");
        _apply("_microm_autoSizeMax", "autoSizeMax");

        return effected;
    }

    // MMC: rows
    useEffect(() => {

        if (!rows?.length) {
            gridRef.current.clear(); //only affects records, calls refresh
            return;
        }

        gridRef.current.clear(true); //only affects records

        // MMC: It is necessary to clear the cache even though the grid's autosize is very slow
        // the column autosize if the grid is not visible does not work, it is necessary to clear the cache if I do refresh manually
        // TODO: change the grid's autosize to one with better performance, keep in mind to update the autosize
        // when records are added to a grid with visible empty rows

        for (let w2colIndex = 0; w2colIndex < gridRef.current.columns.length; w2colIndex++) {
            gridRef.current.columns[w2colIndex]._microm_autoSize_cache = false;
        }

        const w2colFields = gridRef.current.columns
            .filter((w2col: w2column) => w2col.field)
            .map((w2col: w2column) => w2col.field!);

        const w2records = rows.map((row, index) => {
            const w2record: w2record = {
                recid: index + 1 //w2ui has problems when recid is 0
            };
            for (let i = 0; i < w2colFields.length; i++) {
                w2record[w2colFields[i]] = (row as Record<string, unknown>)[w2colFields[i]];
            }
            return w2record;
        });

        gridRef.current.add(w2records); //calls refresh
        //console.debug("useGrid effect records added rows.length", rows.length);

    }, [rows]);

    // MMC: clear cell portals when the rows change
    useEffect(() => {
        //console.debug("useGrid effect clear portals, emptyCellsPortals");
        clearCellPortals();
    }, [clearCellPortals, rows]);


    // MMC: workaround for tabs or when the grid is not visible
    useEffect(() => {
        if (isFirstVisible && autoSizeColumnsOnLoad && rows?.length) {
            //console.debug("useGrid effect, autoSizeColumnsOnLoad, isFirstVisible, rows.length, records.length", autoSizeColumnsOnLoad, isFirstVisible, rows?.length, gridRef.current.records.length);

            performAutosize();

        }
        else {
            //console.debug("useGrid effect MISSED, autoSizeColumnsOnLoad, isFirstVisible, rows.length, records.length", autoSizeColumnsOnLoad, isFirstVisible, rows?.length, gridRef?.current?.records.length);
        }
    }, [rows, autoSizeColumnsOnLoad, isFirstVisible, performAutosize]);

    // MMC: set preserved selection
    useEffect(() => {

        if (rows?.length && preservedSelection.current?.length) {
            //console.debug("useGrid effect rows.length, preservedSeletion", rows?.length, preservedSelection.current?.length);
            gridRef.current.select(preservedSelection.current);
            preservedSelection.current = null;
            gridRef.current.scrollIntoView();
        }

    }, [rows]);

    // MMC: autoSelectFirstRow
    useEffect(() => {

        if (autoSelectFirstRow && rows?.length && !gridRef.current.getSelection().length) {
            gridRef.current.select(1);
        }

    }, [columns, rows, autoSelectFirstRow]);

    // MMC: change theme
    useEffect(() => {
        if (boxRef.current) {
            boxRef.current.classList.remove(colorScheme === "dark" ? "w2grid-light" : "w2grid-dark")
            boxRef.current.classList.add(colorScheme === "dark" ? "w2grid-dark" : "w2grid-light")
        }
    }, [colorScheme]);

    useEffect(() => {
        if (boxRef.current) {
            if (stripped) {
                boxRef.current.classList.add("w2grid-stripped")
            } else {
                boxRef.current.classList.remove("w2grid-stripped")
            }
        }
    }, [stripped]);

    useEffect(() => {
        if (boxRef.current) {
            if (highlightOnHover) {
                boxRef.current.classList.add("w2grid-highlight-hover")
            } else {
                boxRef.current.classList.remove("w2grid-highlight-hover")
            }
        }
    }, [highlightOnHover]);

    useEffect(() => {
        if (boxRef.current) {
            if (columnBorders) {
                boxRef.current.classList.add("w2grid-column-borders")
            } else {
                boxRef.current.classList.remove("w2grid-column-borders")
            }
        }
    }, [columnBorders]);

    useEffect(() => {
        if (boxRef.current) {
            if (rowBorders) {
                boxRef.current.classList.add("w2grid-row-borders")
            } else {
                boxRef.current.classList.remove("w2grid-row-borders")
            }
        }
    }, [rowBorders]);

    useEffect(() => {
        if (boxRef.current) {
            if (withBorder) {
                boxRef.current.classList.add("w2grid-border")
            } else {
                boxRef.current.classList.remove("w2grid-border")
            }
        }
    }, [withBorder]);

    return {
        w2grid: gridRef.current,
        boxRef,
        cellsPortals: Object.values(cellsPortals),
        imperative: useMemo<GridImperative>(() => ({
        }), [])
    };
}
