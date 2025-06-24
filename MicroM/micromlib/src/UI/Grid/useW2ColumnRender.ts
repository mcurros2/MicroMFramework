import { ReactPortal, isValidElement, useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { w2grid } from "../../../libs/w2ui/w2ui.es6";
import { Value } from "../../client";
import { useFirstVisible, useLocaleFormat } from "../Core";
import { GridColumn, GridColumnsOverrides, GridSourceRecord } from "./Grid";
import { w2renderFunction } from "./W2Grid";

export interface UseW2ColumnRenderProps {
    columns?: GridColumn[],
    rows?: GridSourceRecord[],
    columnsOverrides?: GridColumnsOverrides,
    columnAutoResizeSource: React.MutableRefObject<"system" | "user">,
    gridRef: React.MutableRefObject<w2grid>,
    autoSizeColumnsOnLoad?: boolean,
    boxRef: React.RefObject<HTMLDivElement>,
    timeZoneOffset?: number,
}

function escapeHTML(text: string) {
    if (!text) return text;
    return text.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&#039;');
}

const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;


const emptyCellsPortals = {}; //optimization, avoids unnecessary rerenders on multiple portal state cleanups


export function useW2ColumnRender(props: UseW2ColumnRenderProps) {
    const {
        columns, rows, columnsOverrides, columnAutoResizeSource, gridRef, autoSizeColumnsOnLoad, boxRef, timeZoneOffset
    } = props;

    const isFirstVisible = useFirstVisible(boxRef);

    const emptyLocaleProps = useRef({ timeZoneOffset: timeZoneOffset || 0});

    const localeFormat = useLocaleFormat(emptyLocaleProps.current);

    const [cellsPortals, setCellsPortals] = useState<Record<string, ReactPortal>>(emptyCellsPortals);

    const renderedCellsCountRef = useRef(0);
    const renderFirstTime = useRef(false);
    const renderEffectedW2Columns = useRef<number[]>([]);
    const isFirstVisibleRef = useRef(false);

    const autoResizeColumn = useCallback((w2colIndex: number) => {
        if (gridRef.current.columns[w2colIndex]._microm_autoSize_cache) {
            //console.debug("autoResizeColumn cache hit, not resizing", w2colIndex);
            return false;
        }
        columnAutoResizeSource.current = "system";
        try {
            //console.debug("autoResizeColumn resizing", w2colIndex);
            gridRef.current.columnAutoSize(w2colIndex);
        } finally {
            columnAutoResizeSource.current = "user";
        }
        gridRef.current.columns[w2colIndex]._microm_autoSize_cache = true;
        return true;
    }, [columnAutoResizeSource, gridRef]);

    const performAutosize = useCallback((include_render_columns = false) => {
        const _w2refresh = gridRef.current.refresh;
        gridRef.current.refresh = () => undefined;

        let needRefresh = false;

        try {
            gridRef.current.columns.forEach((c, i) => {
                if (include_render_columns === false) {
                    if (!c._microm_render && autoResizeColumn(i)) needRefresh = true;
                }
                else {
                    if (autoResizeColumn(i)) needRefresh = true;
                }
            });
        }
        finally {
            gridRef.current.refresh = _w2refresh;
        }
        if (needRefresh) {
            //console.log('performAutosize refresh - no cache, include_render_columns', include_render_columns);
            gridRef.current.refresh();
        }
        else {
            //console.log('performAutosize refresh - cache hit, include_render_columns', include_render_columns);
        }
    }, [autoResizeColumn, gridRef]);

    const clearCellPortals = useCallback(() => {
        setCellsPortals(emptyCellsPortals);
    }, []);

    // MMC: reset the flag as to render the first time because we need to resize html columns again (workaround)
    useEffect(() => {
        if (isFirstVisible) {
            isFirstVisibleRef.current = true;
            renderFirstTime.current = true;
        }
        //console.debug('useEffect reset the renderFirstTime.current to true');
    }, [columns, rows, columnsOverrides, isFirstVisible]);

    const w2columnRender: w2renderFunction = useCallback(function (record, options) {
        const w2col = options.self.columns[options.colIndex];
        if (!w2col.field) {
            //console.debug('w2columnRender no field', w2col);
            return "";
        }

        const sqlType = columns?.[parseInt(w2col.field)].sqlType;

        let format = w2col._microm_format;

        let value = options.value;

        if (sqlType) {
            if (value === null) {
                value = '';
            }
            else {
                const nativeValue = localeFormat.getNativeValue(value as Value, sqlType);
                const rawValue = localeFormat.formatValue(nativeValue, sqlType);
                value = (rawValue === 'null') ? '' : rawValue;
            }
        }

        //
        switch (format) {
            case "check":
                value = value ? "✓" : "";
                format = "html";
                break;
            case "image":
                value = (value && typeof value === "string") ? `<img class="" src="${encodeURI(value)}" loading="lazy">` : "";
                format = "html";
                break;
            case "url":
                if (value && typeof value === "string") {
                    const encodedValue = encodeURI(value);
                    value = `<a href="${encodedValue}">${escapeHTML(encodedValue)}</a>`;
                    format = "html";
                } else {
                    format = "string";
                }
                break;
            case "email":
                if (value && typeof value === "string" && emailRegex.test(value)) {
                    const encodedValue = encodeURI(value);
                    value = `<a href="mailto:${encodedValue}">${escapeHTML(encodedValue)}</a>`;
                    format = "html";
                } else {
                    format = "string";
                }
                break;
            default:
                break;
        }

        //
        switch (format) {
            case "html":
                break;
            case "string":
                value = typeof value === "string" ? escapeHTML(value) : escapeHTML(String(value));
                break;
            default:
                value = "";
        }

        if (w2col._microm_render && isFirstVisibleRef.current) {
            //TODO: This will create more instances of portals than those on the screen. 
            //Instead of using the recid, a fixed ID from the table containing the virtual scroll should be used (this way there would be a maximum of as many portals as rendered records).
            //As of today, W2UI does not provide the ability to do this(how do we obtain the virtual scroll row index from the recid ?).
            const renderElementID = `microm_grid_column_render_${this.name}_${w2col.field}_${record.recid}`;

            const renderValue = value;
            const recordValue = record;

            // MMC: this reference is trigger autosize after all the cells are rendered
            renderedCellsCountRef.current += 1;

            if (renderEffectedW2Columns.current.indexOf(options.colIndex) === -1) renderEffectedW2Columns.current.push(options.colIndex);

            // MMC: this is to render react elements in the grid, using the render function passed to the column in _microm_render
            // the render for react elements requires two steps, first create a div element with an id, then render the react element in that div (renderElementID)
            setTimeout(function () {
                // MMC: get the div element to render the react element
                const cellElement_current = document.getElementById(renderElementID);

                if (cellElement_current) {

                    // MMC: to avoid flicker, we render the new element in a hidden div, then we replace the old element with the new one
                    cellElement_current.id = `${renderElementID}_old`; // change ID for the old element
                    const cellElement = document.createElement('div'); // new element with the same ID
                    cellElement.id = renderElementID;
                    cellElement.hidden = true;
                    cellElement_current.insertAdjacentElement('afterend', cellElement);

                    // MMC: render the react element in the new div, the render function can return any valid react node, including just text
                    const renderResult = w2col._microm_render!(renderValue, cellElement, recordValue, w2col.field!); //!: ambos ya fueron comprobados
                    if (renderResult !== undefined) {
                        const portalId = `${record.recid}_${w2col.field}`;
                        if (isValidElement(renderResult)) {
                            const portal = createPortal(renderResult, cellElement, portalId);
                            setCellsPortals(prevPortals => ({ ...prevPortals, [portalId]: portal }));
                        } else {
                            // MMC: we need to clean the portal if the render function returned text
                            setCellsPortals(prevPortals => {
                                delete prevPortals[portalId];
                                return prevPortals;
                            });
                            cellElement.replaceChildren(renderResult as string);
                        }
                    }
                    else {
                        // MMC: the render function returned undefined, we clean the cell
                        cellElement.innerHTML = '';
                    }

                    // MMC: remove the old element when everything is rendered (hopefully as there can be complex react components)
                    setTimeout(() => {
                        const cellElement = document.getElementById(renderElementID);
                        if (cellElement) cellElement.hidden = false;
                        const cellOldElement = document.getElementById(`${renderElementID}_old`);
                        if (cellOldElement) cellOldElement.remove();
                    });

                }

                renderedCellsCountRef.current -= 1;

                if (renderedCellsCountRef.current === 0) {
                    //console.debug('RENDER should refresh');
                    if (autoSizeColumnsOnLoad) {
                        if (renderFirstTime.current) {
                            renderFirstTime.current = false;
                            //console.debug('w2columnRender clear cache', renderEffectedW2Columns.current);
                            renderEffectedW2Columns.current.forEach((col_index) => {
                                options.self.columns[col_index]._microm_autoSize_cache = false;
                            });
                            renderEffectedW2Columns.current = [];
                        }
                        setTimeout(() => {
                            performAutosize(true);
                        });
                    }
                }

                //console.debug(
                //    'w2columnRender rendering HTML, renderElementID, renderedCellsRef.current, totalCellsRef.current',
                //    renderElementID, renderedCellsRef.current, totalCellsRef.current);
            });

            // MMC: the idea here is to render the previous element content if it exists, to avoid flicker as the grid detects the string value change and triggers a refresh
            const previousElement = document.getElementById(renderElementID);
            value = previousElement ? previousElement.outerHTML : `<div id="${renderElementID}"></div>`;
        }
        else {
            //console.debug('w2columnRender rendering TEXT, w2col.field', w2col.field);
        }

        return String(value);
    }, [autoSizeColumnsOnLoad, columns, isFirstVisibleRef, localeFormat, performAutosize]);

    return {
        w2columnRender,
        cellsPortals,
        clearCellPortals,
        isFirstVisible,
        performAutosize
    }

}