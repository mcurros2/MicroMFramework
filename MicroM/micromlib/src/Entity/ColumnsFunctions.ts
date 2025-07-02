import { SQLType, Value, ValuesObject } from "../client";
import { SYSTEM_COLUMNS_NAMES } from "./DefaultColumns";
import { EntityColumn } from "./EntityColumn";
import { ColumnsFilter, EntityColumnFlags } from "./EntityColumn.types";
import { ColumnsObject } from "./EntityColumnCollection.types";
import { hasValue } from "./EntityColumnFunctions";
import { isIn } from "./GenericFunctions";

import dayjs from 'dayjs';


export function areArraysContentsEqual(a: string[] | undefined, b: string[] | undefined): boolean {
    if (a === b) return true;
    if (a === undefined && b === undefined) return true;
    if (a == null || b == null) return false;
    if (a.length !== b.length) return false;

    const sortedA = [...a].sort();
    const sortedB = [...b].sort();

    for (let i = 0; i < sortedA.length; i++) {
        if (sortedA[i] !== sortedB[i]) return false;
    }
    return true;
}

export function arrayToObject<T, TValue>(
    values: T[],
    onGetKey: (item: T) => string,
    onGetValue?: (item: T) => TValue
): Record<string, TValue> {
    return values.reduce((result: Record<string, TValue>, item: T) => {
        result[onGetKey(item)] = typeof onGetValue === 'function' ? onGetValue(item) : item as unknown as TValue;
        return result;
    }, {});
}

export function cloneColumns(source_columns: Record<string, EntityColumn<Value>>) {
    const cloned: Record<string, EntityColumn<Value>> = {};
    for (const colname in source_columns) {
        const source = source_columns[colname];
        cloned[colname] = EntityColumn.clone(source);
    }
    return cloned;
}

export function areValuesEqual(values1: ValuesObject, values2: ValuesObject) {
    //TODO: check if are object instances

    if (values1 === values2)
        return true;
    if (!values1 && !values2)
        return true;
    if ((!values1 && values2) || (values1 && !values2))
        return false;

    const keys1 = Object.keys(values1);
    const keys2 = Object.keys(values2);

    if (keys1.length !== keys2.length)
        return false;

    for (const key1 of keys1) {
        if (Object.hasOwnProperty.call(values1, key1)) {
            const value1 = values1[key1];

            let foundKey2 = false;

            for (const key2 of keys2) {
                if (Object.hasOwnProperty.call(values2, key2)) {
                    if (key2 === key1) {
                        const value2 = values2[key2];

                        foundKey2 = true;

                        if (value2 !== value1) {
                            return false;
                        }

                        break;
                    }
                }
            }

            if (!foundKey2)
                return false;
        }
    }
    return true;
}

export function areValuesObjectsEqual(objA: ValuesObject | undefined, objB: ValuesObject | undefined): boolean {
    if (objA === undefined && objB === undefined) return true;
    if (objA === undefined || objB === undefined) return false;

    const keysA = Object.keys(objA);
    const keysB = Object.keys(objB);

    // Check if both objects have the same number of keys
    if (keysA.length !== keysB.length) {
        return false;
    }

    for (const key of keysA) {
        if (!keysB.includes(key)) {
            return false;
        }

        const valueA = objA[key];
        const valueB = objB[key];

        // If both values are Date objects, compare their timestamps
        if (valueA instanceof Date && valueB instanceof Date) {
            if (valueA.getTime() !== valueB.getTime()) {
                return false;
            }
        } else if (Array.isArray(valueA) && Array.isArray(valueB)) {
            if (valueA.length !== valueB.length || !valueA.every((val, index) => val === valueB[index])) {
                return false;
            }
        } else if (valueA !== valueB) {
            return false;
        }
    }

    return true;
}

/**
 * Get a filtered columns array (without duplicates)
 */
export function getColumns(columns: ColumnsObject, filter: ColumnsFilter | null): EntityColumn<Value>[] {
    const result: EntityColumn<Value>[] = [];
    for (const c in columns) {
        const column = columns[c];
        if (result.includes(column)) continue;

        if (filter === null) {
            result.push(column);
        }
        else {
            if (filter.ignoreDefaults) {
                if (column.value === column.defaultValue) continue;
            }
            if (filter.flags === undefined) {
                result.push(column);
            }
            else {
                const matched = (filter.matchAllFlags) ? column.hasFlag(filter.flags) : column.hasAnyFlag(filter.flags);
                if (matched) result.push(column);
            }
        }
    }
    return result;
}

export function filterColumns(columns: EntityColumn<Value>[], filter: ColumnsFilter | null): EntityColumn<Value>[] {
    const result: EntityColumn<Value>[] = [];
    for (let x: number = 0; x++; x < columns.length) {
        const column = columns[x];
        if (result.includes(column)) continue;

        if (filter === null) {
            result.push(column);
        }
        else {
            if (filter.ignoreDefaults) {
                if (column.value === column.defaultValue) continue;
            }
            if (filter.flags === undefined) {
                result.push(column);
            }
            else {
                const matched = (filter.matchAllFlags) ? column.hasFlag(filter.flags) : column.hasAnyFlag(filter.flags);
                if (matched) result.push(column);
            }
        }
    }
    return result;
}



/**
 * Create a ValuesDictionary object containing the matched columns names and values.
 */
export function getValuesObject(columns: ColumnsObject, filters: ColumnsFilter | null) {
    return getValues(columns, filters);
    //return arrayToObject(getColumns(columns, filters), column => column.name, column => column.value);
}

/**
 * Create an array of values containing the matched columns values only.
 */
export function getValuesArray(columns: ColumnsObject, filters: ColumnsFilter | null) {
    return getColumns(columns, filters).map(column => column.value);
}

/**
 * Filter a columns object
 */
export function getColumnsObject(columns: ColumnsObject, filters: ColumnsFilter | null): Record<string, EntityColumn<Value>> {
    return arrayToObject(getColumns(columns, filters), column => column.name);
}

/**
 * Convert a columns array to columnsObject
 */
export function toColumnsObject(columns: EntityColumn<Value>[]): Record<string, EntityColumn<Value>> {
    return arrayToObject(columns, column => column.name);
}

/**
 * Create an array of strings containing the matched columns names only.
 */
export function getColumnsNamesArray(columns: ColumnsObject, filters: ColumnsFilter | null) {
    return getColumns(columns, filters).map(column => column.name);
}


/**
 * Create a dictionary-like object containing the column names and values.
 */
export function getValues(columns: ColumnsObject, filter: ColumnsFilter | null): Record<string, Value> {
    const result: Record<string, Value> = {};
    for (const c in columns) {
        const column = columns[c];

        if (filter === null) {
            result[column.name] = column.value;
        }
        else {
            if (filter.ignoreSystemColumns) {
                if (SYSTEM_COLUMNS_NAMES.includes(c)) continue;
            }
            if (filter.ignoreDefaults) {
                if (column.value === column.defaultValue) continue;
            }
            if (filter.flags === undefined) {
                result[column.name] = column.value;
            }
            else {
                const matched = (filter.matchAllFlags) ? column.hasFlag(filter.flags) : column.hasAnyFlag(filter.flags);
                if (matched) {
                    result[column.name] = column.value;
                }
            }
        }
    }
    return result;
}

export function setSourceValue(columnName: string, sourceValue: Value | EntityColumn<Value>, columns: Partial<Record<string, EntityColumn<Value>>>, ignoreInexistentColumns: boolean, ignoreEmptySourceValues: boolean, convertStringArray: boolean) {
    const column = columns[columnName];
    if (!column) {
        if (!ignoreInexistentColumns) throw new Error(`Column '${columnName}' not found.`);
        return;
    }

    let valueToSet: Value;
    let valueDescriptionToSet: string | undefined;

    if (sourceValue instanceof EntityColumn) {
        valueToSet = sourceValue.value;
        valueDescriptionToSet = sourceValue.valueDescription;
    } else {
        valueToSet = sourceValue;
    }

    // Convert string to Date if necessary
    if (isIn<SQLType>(column.type, 'date', 'datetime', 'datetime2', 'smalldatetime') && typeof valueToSet === 'string' && !isIn(columnName, 'dt_lu')) {
        try {
            // FIX to handle dayjs issue parsing string ISO 8601 with trimmed zeroes at the end of the milliseconds
            // 2025-07-02T16:58:45.57 57 here means 570 milliseconds but dayJS has a bug and parses it as 57 milliseconds

            const endsWithDotAndTwoDigits = /\.\d{2}$/;

            if (valueToSet.length === 22 && endsWithDotAndTwoDigits.test(valueToSet)) {
                valueToSet = valueToSet + '0'; // add the missing zero to milliseconds
            }

            // dayjs is used here for compatibility with mantine calendar component which formats the date with dayjs
            valueToSet = dayjs(valueToSet).toDate();

        }
        catch (e) {
            console.log(`Error converting string to Date: ${e}`, e);
        }
    }

    if (!hasValue(valueToSet) && ignoreEmptySourceValues) return;

    // MMC: specific for entityAPI getData method, where the json array comes as an escaped string
    if (convertStringArray && column.isArray && typeof valueToSet === "string") {
        column.value = JSON.parse(valueToSet);
    }
    else {
        column.value = valueToSet;
    }

    if (valueDescriptionToSet) column.valueDescription = valueDescriptionToSet;
}

/**
 * Sets matched columns values from any supported source type.
 * By default, all columns are matched and throws error if column is not found.
 */
export function setValues(columns: ColumnsObject, sourceValues?: ValuesObject | ColumnsObject | EntityColumn<Value>[], filters?: ColumnsFilter | null, ignoreInexistentColumns: boolean = false, ignoreEmptySourceValues: boolean = false, convertStringArray: boolean = false) {
    if (!sourceValues) return;
    if (filters === undefined) filters = null;
    const matchedColumns = getColumnsObject(columns, filters);

    if (Array.isArray(sourceValues)) {
        for (let sourceIndex = 0; sourceIndex < sourceValues.length; sourceIndex++) {
            const sourceColumn = sourceValues[sourceIndex];
            setSourceValue(sourceColumn.name, sourceColumn.value, matchedColumns, ignoreInexistentColumns, ignoreEmptySourceValues, convertStringArray);
        }
    } else {
        for (const columnName in sourceValues) {
            if (!Object.hasOwnProperty.call(sourceValues, columnName)) continue;
            const sourceValue: Value | EntityColumn<Value> | undefined = sourceValues[columnName];
            setSourceValue(columnName, sourceValue!, matchedColumns, ignoreInexistentColumns, ignoreEmptySourceValues, convertStringArray);
        }
    }
}

/**
 * Clears the matched columns values.
 */
export function clearValues(columns: ColumnsObject, useColumnDefaultValue: boolean = true, filters: ColumnsFilter | null = null) {
    getColumns(columns, filters).forEach(column => column.value = useColumnDefaultValue ? column.defaultValue : '');
}


/**
 * Sets the matched columns values using the entity parent keys as source.
 * By default, only columns with PKFlag or FKFlag set are affected and if the parent key value is empty, then the column is not affected.
 */
export function fillParentKeys(columns: ColumnsObject, parentKeys: ColumnsObject, ignoreEmptyParentKeys: boolean = true, filters: ColumnsFilter | null = null) {
    if (filters === null) {
        filters = {
            flags: EntityColumnFlags.pk | EntityColumnFlags.fk,
            ignoreDefaults: false
        }
    }
    setValues(columns, parentKeys, filters, true, ignoreEmptyParentKeys);
}

export function namesOf<T>() {
    return new Proxy(
        {},
        {
            get: function (_target, prop, _receiver) {
                return prop;
            },
        }
    ) as {
            [P in keyof T]: P;
        };
};

export function copyValuesObject(obj: ValuesObject): ValuesObject {
    const copiedObject: ValuesObject = {};

    for (const key in obj) {
        if (Object.prototype.hasOwnProperty.call(obj, key)) {
            const value = obj[key];

            // Handle Date object separately
            if (value instanceof Date) {
                copiedObject[key] = new Date(value.getTime()) as Value;
            } else {
                copiedObject[key] = value;
            }
        }
    }

    return copiedObject;
}
