import { ColumnsObject, DefaultColumnsNames } from "../../Entity";
import { ValuesObject } from "../../client";

export function getMantineInitialDirty(columns: ColumnsObject, exclude_system_columns: boolean) {
    const i: Record<string, boolean> = {};
    Object.entries(columns).forEach(([key, column]) => {
        if (exclude_system_columns && DefaultColumnsNames.includes(key)) return;
        i[key] = (column.value !== '' || column.value !== null || column.value !== undefined) ? true : false;
    });
    return i;
}

export function getMantineValuesObject(formValues: ValuesObject, columns: ColumnsObject, exclude_system_columns: boolean) {
    const i: ValuesObject = {};
    const formColumnNames = Object.keys(formValues);
    Object.entries(columns).forEach(([key, column]) => {
        if (exclude_system_columns && DefaultColumnsNames.includes(key)) return;
        // MMC: just set the values for the binded columns
        if (formColumnNames.includes(key)) i[key] = column.value ?? '';
    });
    return i;
}

export function getMantineInitialValuesObject(columns: ColumnsObject, columnNames?: string[]) {
    const i: ValuesObject = {};
    if(!columnNames || columnNames.length === 0) return i;
    Object.entries(columns).forEach(([key, column]) => {
        if (columnNames.includes(key)) i[key] = column.value ?? '';
    });
    return i;
}

