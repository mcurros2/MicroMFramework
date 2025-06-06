import ExcelJS from 'exceljs';
import { DataResult, SQLType, Value, ValuesObject, ValuesRecord } from "../client";
import { namesOf } from "./ColumnsFunctions";

export type MappingBehavior = 'lax' | 'enforceObject' | 'enforceDataResult' | 'exactMatch';

// MMC: function to convert a header to camel case
export function toCamelCase(str: string): string {
    return str
        // Replace spaces, accents, or '.' followed by any character with the uppercase version of the character
        .replace(/[\s.\u00C0-\u00FF]+(.)/g, (m, chr) => chr.toUpperCase())
        // Ensure the first character is lowercase
        .replace(/^./, (m) => m.toLowerCase());
}

/** function to convert a record array to a values object */
export function convertRecordToValuesObject(
    record: ValuesRecord,
    headers: string[],
    typeInfo: SQLType[],
    formatToString?: (value: Value, sqlType: SQLType) => string
): ValuesObject {
    const recordObject = {} as ValuesObject;

    headers.forEach((header, index) => {
        (recordObject as ValuesObject)[header] = formatToString ? formatToString(record[index], typeInfo[index]) : record[index];
    });

    return recordObject;
}

// MMC: function to convert a data result to ValuesObject[]
export function convertRecordsToArrayOfValuesObject(
    dataResult: DataResult,
    nameTransformFunction: ((name: string) => string) | null = toCamelCase,
    formatToString?: (value: Value) => string
): ValuesObject[] {
    const headers = nameTransformFunction ? dataResult.Header.map(nameTransformFunction) : dataResult.Header; // transform header names

    return dataResult.records.map((record) => convertRecordToValuesObject(record, headers, dataResult.typeInfo, formatToString));
}


// MMC: Generic function to convert a DataResult record to T
export function mapRecordToType<T extends ValuesObject>(
    record: ValuesRecord,
    headers: string[],
    enforceObjectKeys: Set<string>,
    mapping_behavior: MappingBehavior,
    map_properties: Record<string, keyof T> | null = null,
    skipUndefinedOrNull: boolean = false
): T {
    const mappedRecord: Partial<T> = {};

    headers.forEach((header, index) => {
        const valueExists = index < record.length;

        const mappedKey = map_properties ? map_properties[header] : header;

        if ((valueExists || mapping_behavior === 'lax') && mappedKey !== undefined) {
            const value = valueExists ? record[index] as T[keyof T] : undefined;
            if (skipUndefinedOrNull && (value === undefined || value === null)) return;
            mappedRecord[mappedKey as keyof T] = value;
        }
        else if (mapping_behavior === 'enforceObject' && !enforceObjectKeys.has(header)) {
            throw new Error(`Key ${header} from record is missing in object T`);
        }
    });

    return mappedRecord as T;
}

// MMC: generic function to convert a DataResult to an array of T
export function mapDataResultToType<T extends ValuesObject>(
    dataResult: DataResult,
    mapping_behavior: MappingBehavior,
    nameTransformFunction: ((name: string) => string) | null = toCamelCase,
    map_properties: Record<string, keyof T> | null = null,
    skipUndefinedOrNull: boolean = false
): T[] {
    const headers = nameTransformFunction ? dataResult.Header.map(nameTransformFunction) : dataResult.Header; // transform header names
    const headersSet = new Set(headers);
    const objectKeys = namesOf<T>();
    const enforceObjectKeysSet = new Set(Object.keys(objectKeys));

    // Perform checks before mapping
    if (mapping_behavior === 'enforceDataResult' || mapping_behavior === 'exactMatch') {
        const missingInT = headers.filter(header => !enforceObjectKeysSet.has(header));
        if (missingInT.length > 0) {
            throw new Error(`Some DataResult headers are not present in object T. Missing headers: ${missingInT.join(', ')}`);
        }
    }

    if (mapping_behavior === 'exactMatch') {
        const missingInHeaders = Array.from(enforceObjectKeysSet).filter(key => !headersSet.has(key));
        if (missingInHeaders.length > 0) {
            throw new Error(`Some object T keys are not present in DataResult headers. Missing keys: ${missingInHeaders.join(', ')}`);
        }
    }

    // Map each record
    return dataResult.records.map(record =>
        mapRecordToType<T>(record, headers, enforceObjectKeysSet, mapping_behavior, map_properties, skipUndefinedOrNull)
    );
}


// MMC: function to export a dataresult to csv
export function exportToCSV(viewResult: DataResult, notExportableColumns?: number[]) {
    if (!viewResult.Header.length) return;

    const columns = viewResult.Header;
    const rows = viewResult.records;

    const fechaActual = new Date();
    const filename = `export-${fechaActual.getFullYear()}${(fechaActual.getDate() + '').padStart(2, '0')}${(fechaActual.getMonth() + 1 + '').padStart(2, '0')}_${(fechaActual.getHours() + '').padStart(2, '0')}${(fechaActual.getMinutes() + '').padStart(2, '0')}${(fechaActual.getSeconds() + '').padStart(2, '0')}.csv`;

    const row_sep = '\r\n';
    const col_sep = ';';
    let csv_data = '';

    // MMC: csv headers
    for (let r = 0; r < columns.length; r++) {
        // MMC: check if notExportableColumns is defined and if the column is not exportable
        if (notExportableColumns && notExportableColumns.includes(r)) continue;
        csv_data += '"' + columns[r].replace(/"/g, '""') + '"' + col_sep;
    }
    csv_data += row_sep;
    for (let r = 0; r < rows.length; r++) {
        for (let c = 0; c < columns.length; c++) {
            if (notExportableColumns && notExportableColumns.includes(c)) continue;
            const col_data = rows[r][c];
            if (typeof col_data === 'string' || col_data instanceof String) col_data.replace(/"/g, '""');
            csv_data += '"' + col_data + '"' + col_sep;
        }
        csv_data += row_sep;
    }

    // MMC: \ufeff = BOM for utf8. Without this excel won't open the file correctly
    csv_data = '\ufeff' + csv_data;

    // MMC: some security checks can cause an exception
    try {
        if (window.Blob && window.URL) {
            const blob = new Blob([csv_data], {
                type: 'text/csv;charset=utf-8'
            });
            const csv_url = URL.createObjectURL(blob);
            const linkElement = document.createElement('a');
            linkElement.setAttribute('download', filename);
            linkElement.setAttribute('href', csv_url);
            linkElement.click();

            // MMC: cleanup
            document.body.removeChild(linkElement);
            URL.revokeObjectURL(csv_url);
        }
        else {
            console.error("Your browser does not support exporting data.");
        }
    } catch (ex) {
        console.error("exportToCSV", ex);
    }
}

export async function exportToExcel(data: DataResult[], notExportableColumns?: number[], sheetNames?: string[]) {
    // MMC: some security checks can cause an exception
    try {
        if (window.Blob && window.URL) {
            const currentDate = new Date();
            const filename = `export-${currentDate.getFullYear()}${(currentDate.getDate() + '').padStart(2, '0')}${(currentDate.getMonth() + 1 + '').padStart(2, '0')}_${(currentDate.getHours() + '').padStart(2, '0')}${(currentDate.getMinutes() + '').padStart(2, '0')}${(currentDate.getSeconds() + '').padStart(2, '0')}.xlsx`;

            const workbook = new ExcelJS.Workbook();
            data.forEach((result, index) => {
                const worksheet = workbook.addWorksheet(sheetNames ? sheetNames[index] : `Data ${index + 1}`);

                const exportableColumnsConfig: { header: string; key: string; originalIndex: number }[] = [];
                result.Header.forEach((header, colIndex) => {
                    if (!notExportableColumns?.includes(colIndex)) {
                        exportableColumnsConfig.push({ header, key: header, originalIndex: colIndex });
                    }
                });

                worksheet.columns = exportableColumnsConfig.map(col => ({ header: col.header, key: col.key }));

                exportableColumnsConfig.forEach(colInfo => {
                    const originalColIndex = colInfo.originalIndex;
                    const columnType = result.typeInfo[originalColIndex];
                    const column = worksheet.getColumn(colInfo.key);

                    if (column) {
                        switch (columnType) {
                            case 'tinyint':
                            case 'smallint':
                            case 'int':
                            case 'bigint':
                                column.numFmt = '0';
                                break;
                            case 'float':
                            case 'decimal':
                            case 'real':
                                column.numFmt = '#,##0.00';
                                break;
                            case 'money':
                                column.numFmt = '$#,##0.00';
                                break;
                            case 'bit':
                                column.numFmt = '0';
                                break;
                            case 'date':
                                column.numFmt = 'yyyy-mm-dd';
                                break;
                            case 'datetime':
                            case 'datetime2':
                            case 'smalldatetime':
                                column.numFmt = 'yyyy-mm-dd hh:mm:ss';
                                break;
                            case 'time':
                                column.numFmt = 'hh:mm:ss';
                                break;
                        }
                    }
                });

                result.records.forEach(row => {
                    const rowForExcel: ValuesObject = {};
                    exportableColumnsConfig.forEach(colInfo => {
                        const originalValue = row[colInfo.originalIndex];
                        let processedValue = originalValue;

                        const columnType = result.typeInfo[colInfo.originalIndex];

                        if (processedValue !== null) {
                            switch (columnType) {
                                case 'date':
                                case 'datetime':
                                case 'datetime2':
                                case 'smalldatetime':
                                case 'time':
                                    if (typeof processedValue === 'string') {
                                        try {
                                            const dateObj = new Date(processedValue);
                                            if (!isNaN(dateObj.getTime())) {
                                                processedValue = dateObj;
                                            } else {
                                                console.warn(`Invalid date string "${processedValue}" for column "${colInfo.header}". Keeping as string.`);
                                            }
                                        } catch (e) {
                                            console.error(`Error parsing date string "${processedValue}" for column "${colInfo.header}":`, e);
                                        }
                                    }
                                    break;
                                case 'bit':
                                    if (processedValue === 0) {
                                        processedValue = false;
                                    } else if (processedValue === 1) {
                                        processedValue = true;
                                    }
                                    break;
                            }
                        }

                        rowForExcel[colInfo.key] = processedValue;
                    });
                    worksheet.addRow(rowForExcel);
                });

            });

            const buffer = await workbook.xlsx.writeBuffer({ useStyles: true });
            const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;

            document.body.appendChild(a);
            a.click();

            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        }
        else {
            console.error("Your browser does not support exporting data.");
        }
    } catch (ex) {
        console.error("exportToExcel", ex);
    }
}

export function extractArrayFromSelectedRecords<T>(selectedKeys: ValuesObject[], property: keyof ValuesObject): T[] {
    return selectedKeys.map(item => item[property]) as T[];
}