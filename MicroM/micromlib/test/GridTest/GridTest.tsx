import { useId, useMemo, useRef, useState } from "react";
import { DefaultGridProps, Grid, GridColumn, GridColumnsOverrides, GridImperative, GridSourceRecord } from "../../src";
import { RandomRecords, RandomDate, RandomFullName, RandomIntFromInterval, RandomHexColor } from "../RandomData";
import { IconCheck } from "@tabler/icons-react";
import { Text } from "@mantine/core";

function RandomRecord() : GridSourceRecord {
    return [
        "https://i.pravatar.cc/64?u=" + RandomIntFromInterval(0, 99999999).toString(),
        RandomFullName(),
        RandomDate().toLocaleDateString('es-AR'),
        RandomIntFromInterval(12345678, 22345678),
        "$ " + RandomIntFromInterval(150000, 500000) + "." + RandomIntFromInterval(0, 99),
        !!RandomIntFromInterval(0, 1),
        "<div style='background: linear-gradient(90deg, #" + RandomHexColor() + " 0%, #" + RandomHexColor() + " 35%, #" + RandomHexColor() + " 100%);'>H<b>TM</b>L</div>",
        "<div style='background: linear-gradient(90deg, #" + RandomHexColor() + " 0%, #" + RandomHexColor() + " 35%, #" + RandomHexColor() + " 100%);'>H<b>TM</b>L</div>",
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
    ];
}

const exampleRecords1 = 1000;
const exampleRecords2 = 25000;

function CheckMark({checkColor}:{checkColor:string}) {
    const id = useId();


    return <>
            <IconCheck color={checkColor} style={{verticalAlign: "middle"}}></IconCheck>
            <Text display={"inline"} size={"xs"} color="dimmed">({id})</Text>
        </>;
}

export function GridTest() {
    const gridRef = useRef<GridImperative>(null!);
    const [checkColor, setCheckColor] = useState("red");
    const [showAvatarsColumn, setShowAvatarsColumn] = useState(false);

    const [columns] = useState<GridColumn[]>([
        { text: 'Avatar', field: "0", format: "image" },
        { text: "Apellido y nombre", field: "1" },
        { text: "Fecha de nacimiento", field: "2" },
        { text: "DNI", field: "3" },
        { text: "Sueldo", field: "4" },
        { text: "Habilitado", field: "5", format: "html" },
        { text: "No HTML", field: "6" },
        { text: "HTML", field: "7", format: "html" },
        { text: "Historia", field: "8" },
    ]);

    const columnsOverrides = useMemo<GridColumnsOverrides>(() => ({
        "0": {
            hidden: !showAvatarsColumn
        },
        "5": {
            render: value => value ? <CheckMark checkColor={checkColor}></CheckMark> : undefined
        },
    }), [showAvatarsColumn, checkColor]);

    const dataSource1 = useRef(RandomRecords(exampleRecords1, RandomRecord));
    const dataSource2 = useRef(RandomRecords(exampleRecords2, RandomRecord));
    const [rows, setRows] = useState<GridSourceRecord[] | undefined>(dataSource2.current);
    const [preserveSelection, setPreserveSelection] = useState(DefaultGridProps.preserveSelection);
    const [showSelectCheckbox, setShowSelectCheckbox] = useState(DefaultGridProps.showSelectCheckbox);
    const [bigRows, setBigRows] = useState(false);
    const [selectionMode, setSelectionMode] = useState(DefaultGridProps.selectionMode);
    const [stripped, setStripped] = useState(DefaultGridProps.stripped);
    const [highlightOnHover, setHighlightOnHover] = useState(DefaultGridProps.highlightOnHover);
    const [columnBorders, setColumnBorders] = useState(DefaultGridProps.columnBorders);
    const [rowBorders, setRowBorders] = useState(DefaultGridProps.rowBorders);
    const [withBorder, setWithBorder] = useState(DefaultGridProps.withBorder);
    const [autoSizeColumnsOnLoad, setAutoSizeColumnsOnLoad] = useState(DefaultGridProps.autoSizeColumnsOnLoad);
    const [autoSelectFirstRow, setAutoSelectFirstRow] = useState(DefaultGridProps.autoSelectFirstRow);

    return <>
        <Grid ref={gridRef} preserveSelection={preserveSelection} showSelectCheckbox={showSelectCheckbox}
            selectionMode={selectionMode} rows={rows} columns={columns} rowHeight={bigRows ? 74 : undefined}
            onDoubleClick={record => console.info("onDoubleClick", record)}
            onSelectionChanged={selection => console.info("onSelectionChanged", selection)}
            stripped={stripped} highlightOnHover={highlightOnHover} columnBorders={columnBorders} rowBorders={rowBorders}
            withBorder={withBorder} autoSizeColumnsOnLoad={autoSizeColumnsOnLoad} columnsOverrides={columnsOverrides}
            autoSelectFirstRow={autoSelectFirstRow}
        />
        <div>Rows: {rows?.length || 0}</div>
        <div style={{marginTop: "1rem"}}>
            <div>Data source:</div>
            <label>
                <input type="radio" name="data source" checked={rows === undefined} onChange={() => setRows(undefined)} />Empty
                <input type="radio" name="data source" checked={rows === dataSource1.current} onChange={() => setRows(dataSource1.current)} />1
                <input type="radio" name="data source" checked={rows === dataSource2.current} onChange={() => setRows(dataSource2.current)} />2
            </label>
        </div>
        <div style={{marginTop: "1rem"}}>
            <div>State:</div>
            <label>
                <input type="checkbox" checked={preserveSelection} onChange={() => setPreserveSelection(prevState => !prevState)} />preserveSelection
            </label>
            <label>
                <input type="checkbox" checked={showSelectCheckbox} onChange={() => setShowSelectCheckbox(prevState => !prevState)} />showSelectCheckbox
            </label>
            <label>
                <input type="checkbox" checked={selectionMode === "multi"} onChange={() => setSelectionMode(prevState => prevState === "single" ? "multi" : "single")} />Multiselect
            </label>
            <label>
                <input type="checkbox" checked={stripped} onChange={() => setStripped(prevState => !prevState)} />stripped
            </label>
            <label>
                <input type="checkbox" checked={highlightOnHover} onChange={() => setHighlightOnHover(prevState => !prevState)} />highlightOnHover
            </label>
            <label>
                <input type="checkbox" checked={columnBorders} onChange={() => setColumnBorders(prevState => !prevState)} />columnBorders
            </label>
            <label>
                <input type="checkbox" checked={rowBorders} onChange={() => setRowBorders(prevState => !prevState)} />rowBorders
            </label>
            <label>
                <input type="checkbox" checked={withBorder} onChange={() => setWithBorder(prevState => !prevState)} />withBorder
            </label>
            <label>
                <input type="checkbox" checked={autoSizeColumnsOnLoad} onChange={() => setAutoSizeColumnsOnLoad(prevState => !prevState)} />autoSizeColumnsOnLoad
            </label>
            <label>
                <input type="checkbox" checked={autoSelectFirstRow} onChange={() => setAutoSelectFirstRow(prevState => !prevState)} />autoSelectFirstRow
            </label>
        </div>

        <div style={{marginTop: "1rem"}}>
            <div>Examples:</div>
            <label>
                <input type="checkbox" checked={showAvatarsColumn} onChange={() => setShowAvatarsColumn(prevState => !prevState)} />Avatars column
            </label>
            <label>
                <input type="checkbox" checked={bigRows} onChange={() => setBigRows(prevState => !prevState)} />Big rows
            </label>
            <label>
                <input type="checkbox" checked={checkColor === "red"} onChange={() => setCheckColor(prevColor => prevColor === "red" ? "green" : "red")} />Check mark red
            </label>
        </div>

        <div style={{marginTop: "1rem"}}>
            <div>Data:</div>
            <button onClick={() => setRows(prevRows => [...(prevRows||[]), RandomRecord()])}>Add row</button>
            <button onClick={() => rows?.length && setRows([...rows.slice(0, -1)])}>Remove row</button>
            <button onClick={() => setRows(prevRows => [...(prevRows||[]), ...RandomRecords(100, RandomRecord)])}>Add 100 rows</button>
            <button onClick={() => rows?.length && setRows([...rows.slice(0, -100)])}>Remove 100 rows</button>
        </div>


    </>

}