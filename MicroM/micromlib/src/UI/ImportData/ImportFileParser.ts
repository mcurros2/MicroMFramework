import ExcelJS from "exceljs";
import { Cell, CellError, readXls } from "xls-reader";

export interface ImportFileSheet {
    name: string | null,
    rows: string[][],
}

export interface ParsedImportFile {
    format: 'csv' | 'xls' | 'xlsx',
    sheets: ImportFileSheet[],
}

export function getImportFileExtension(file: File) {
    const separator = file.name.lastIndexOf('.');
    return separator >= 0 ? file.name.slice(separator).toLowerCase() : '';
}

function hasContent(row: readonly string[]) {
    return row.some(value => value.trim() !== '');
}

export function parseCSVRows(text: string) {
    const rows: string[][] = [];
    let row: string[] = [];
    let field = '';
    let inQuotes = false;

    const pushField = () => {
        row.push(field);
        field = '';
    };

    const pushRow = () => {
        pushField();
        rows.push(row);
        row = [];
    };

    for (let index = 0; index < text.length; index++) {
        const character = text[index];

        if (inQuotes) {
            if (character === '"') {
                if (text[index + 1] === '"') {
                    field += '"';
                    index++;
                }
                else {
                    inQuotes = false;
                }
            }
            else {
                field += character;
            }
            continue;
        }

        if (character === '"' && field.length === 0) {
            inQuotes = true;
        }
        else if (character === ',') {
            pushField();
        }
        else if (character === '\n') {
            pushRow();
        }
        else if (character !== '\r') {
            field += character;
        }
    }

    if (inQuotes) throw new Error('The CSV file contains an unterminated quoted value.');
    if (field.length > 0 || row.length > 0) pushRow();

    if (rows[0]?.[0]) rows[0][0] = rows[0][0].replace(/^\uFEFF/, '');
    return rows.filter(hasContent);
}

function displayCellValue(value: unknown) {
    if (value === null || value === undefined) return '';
    if (value instanceof Date) return value.toISOString();
    if (value instanceof CellError) return value.code;
    return String(value);
}

function excelWorksheetRows(worksheet: ExcelJS.Worksheet) {
    const rows: string[][] = [];
    for (let rowIndex = 1; rowIndex <= worksheet.rowCount; rowIndex++) {
        const worksheetRow = worksheet.getRow(rowIndex);
        rows.push(Array.from(
            { length: worksheetRow.cellCount },
            (_, columnIndex) => worksheetRow.getCell(columnIndex + 1).text
        ));
    }
    return rows;
}

function legacyWorksheetRows(rows: ReadonlyArray<ReadonlyArray<Cell>>) {
    return rows.map(row => row.map(displayCellValue));
}

export async function parseImportFile(file: File): Promise<ParsedImportFile> {
    const extension = getImportFileExtension(file);

    if (extension === '.csv') {
        return {
            format: 'csv',
            sheets: [{ name: null, rows: parseCSVRows(await file.text()) }]
        };
    }

    if (extension === '.xlsx') {
        const workbook = new ExcelJS.Workbook();
        await workbook.xlsx.load(await file.arrayBuffer());
        return {
            format: 'xlsx',
            sheets: workbook.worksheets.map(worksheet => ({
                name: worksheet.name,
                rows: excelWorksheetRows(worksheet)
            }))
        };
    }

    if (extension === '.xls') {
        const workbook = readXls(await file.arrayBuffer());
        return {
            format: 'xls',
            sheets: workbook.sheets
                .filter(sheet => sheet.visibility === 'visible')
                .map(sheet => ({
                    name: sheet.name,
                    rows: legacyWorksheetRows(sheet.rows)
                }))
        };
    }

    throw new Error(`Unsupported import file extension '${extension || '(none)'}.`);
}
