import { Value, ValuesObject } from "../client";
import { setSourceValue } from "./ColumnsFunctions";
import { Entity } from "./Entity";
import { ColumnsObject } from "./EntityColumnCollection.types";
import { EntityDefinition } from "./EntityDefinition";

export interface ColumnMapRecord {
    columnName: string;
    propertyName: string;
}

export type ColumnMap = ColumnMapRecord[];

export interface ColumnsMapRecord<T extends Partial<ColumnsObject>, V> {
    columnName: keyof T,
    propertyName: keyof V,
}

export type ColumnsMap<T extends Partial<ColumnsObject>, V> = ColumnsMapRecord<T, V>[];

/**
 * Returns a dictionary of column names mapped to property names.
 * @param map
 * @returns
 */
export function getDictionaryByColumnName<T extends Partial<ColumnsObject>, V>(map: ColumnsMap<T, V>): Record<keyof T, keyof V> {
    const dictionary = {} as Record<keyof T, keyof V>;
    map.forEach(record => {
        dictionary[record.columnName] = record.propertyName;
    });
    return dictionary;
}

/**
 * Returns a dictionary of property names mapped to column names.
 * @param map
 * @returns
 */
export function getDictionaryByPropertyName<V, T extends Partial<ColumnsObject>>(map: ColumnsMap<T, V>): Record<keyof V, keyof T> {
    const dictionary = {} as Record<keyof V, keyof T>;
    map.forEach(record => {
        dictionary[record.propertyName] = record.columnName;
    });
    return dictionary;
}

export function mapToRecordSelection<T extends Partial<ColumnsObject>, V>(values: V[], propertiesMapDictionary: Record<keyof V, keyof T>) {
    const result: ValuesObject[] = [];
    const valueNames = Object.keys(propertiesMapDictionary) as (keyof V)[];
    const columnNames = valueNames.map(
        (valueName) => propertiesMapDictionary[valueName] as string
    );
    const valuesLength = values.length;
    const valueNamesLength = valueNames.length;

    for (let i = 0; i < valuesLength; i++) {
        const value = values[i];
        const selectedRecord: ValuesObject = {};

        for (let j = 0; j < valueNamesLength; j++) {
            selectedRecord[columnNames[j]] = value[valueNames[j]] as Value;
        }

        result.push(selectedRecord);
    }

    return result;
}

export class ColumnsMapper<T extends Entity<EntityDefinition>, V extends ValuesObject> {
    #records: ColumnMapRecord[];
    #columnsDictionary: Record<string, string> = {};
    #propertiesDictionary: Record<string, string> = {};

    #columnKeys: Set<string>;
    #valueKeys: Set<string>;

    constructor(entity: T, values: V, records: ColumnMapRecord[]) {
        // check if columnNames exists in entity.def.columns
        records.forEach(record => {
            if (!entity.def.columns[record.columnName])
                throw new Error(`Invalid column map (col: '${record.columnName}'): Column '${record.columnName}' in  not found.`);
        });

        // check if propertyNames exists in values
        records.forEach(record => {
            if (!values[record.propertyName])
                throw new Error(`Invalid value map (value: '${record.propertyName}'): Value '${record.propertyName}' in  not found.`);
        });

        this.#records = records;
        this.#columnKeys = new Set(Object.keys(entity.def.columns));
        this.#valueKeys = new Set(Object.keys(values));

    }

    #getDictionaryByColumnName(): Record<string, string> {
        const dictionary = {} as Record<string, string>;
        this.#records.forEach(record => {
            dictionary[record.columnName] = record.propertyName;
        });
        return dictionary;
    }

    #getDictionaryByPropertyName(): Record<string, string> {
        const dictionary = {} as Record<string, string>;
        this.#records.forEach(record => {
            dictionary[record.propertyName] = record.columnName;
        });
        return dictionary;
    }

    columnsDictionary: Record<string, string> = this.#columnsDictionary ??= this.#getDictionaryByColumnName();
    valuesDictionary: Record<string, string> = this.#propertiesDictionary ??= this.#getDictionaryByPropertyName();

    mapValuesToColumns(columns: ColumnsObject, values: V, ignoreInvalidMappings: boolean = false, ignoreInexistentValues: boolean = true) {
        for (const valueName in values) {
            const columnName = this.valuesDictionary[valueName] as string | undefined;

            if (!columnName) {
                if (!ignoreInexistentValues) throw new Error(`Invalid value map (value: '${valueName}'): is not mapped.`);
                continue;
            }

            const column = columns[columnName];

            if (!column && !ignoreInvalidMappings)
                throw new Error(`Invalid value map (value: '${valueName}', col: '${columnName}'): Column '${columnName}' in  not found.`);

            setSourceValue(columnName, values[valueName], columns, ignoreInvalidMappings, true, false);
        }
    }

    mapColumnsToValues(columns: ColumnsObject, ignoreInvalidMappings: boolean = false, ignoreInexistentValues: boolean = true): Partial<V> {
        const values: Partial<V> = {};
        for (const columnName in columns) {
            const valueName = this.columnsDictionary[columnName] as string | undefined;

            if (!valueName) {
                if (!ignoreInexistentValues) throw new Error(`Invalid column map (col: '${columnName}'): is not mapped.`);
                continue;
            }
            const value = values[valueName];

            if (!value && !ignoreInvalidMappings)
                throw new Error(`Invalid value map (col: '${columnName}', value: '${valueName}'): Value '${valueName}' in  not found.`);

            values[valueName as keyof V] = columns[columnName].value as V[keyof V];
        }
        return values;
    }

}


