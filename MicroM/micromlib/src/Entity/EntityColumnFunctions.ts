import { Value } from "../client";
import { EntityColumn } from "./EntityColumn";

export function hasValue(value: Value) {
    return value !== '' && value !== null && typeof value !== 'undefined';
}

export function convertValueFromString(col: EntityColumn<Value>, value: string): Value {
    if (['float', 'decimal', 'real', 'money', 'smallmoney'].includes(col.type)) {
        return parseFloat(value);
    }
    if (['datetime', 'date'].includes(col.type)) {
        return new Date(value);
    }
    if (col.type === "bit") {
        return value === 'true';
    }
    if (['int', 'bigint'].includes(col.type)) {
        return parseInt(value);
    }
    if (['char', 'nchar', 'varchar', 'nvarchar', 'text', 'ntext'].includes(col.type) && col.isArray) {
        // convert from json string []
        return JSON.parse(value);
    }

    return value;
}

