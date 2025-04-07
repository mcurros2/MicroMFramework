import { ColumnsObject, convertValueFromString } from "../../Entity";
import { Value, ValuesObject } from "../../client";

export function getWindowLocationQueryStringAsObject(): Record<string, string> {
    const searchParams = new URLSearchParams(window.location.search);
    const params: Record<string, string> = {};
    for (const [key, value] of searchParams) {
        params[key] = value;
    }
    return params;
}

export function buildURLQueryString(params: Record<string, Value>): string {
    const searchParams = new URLSearchParams();
    for (const key in params) {
        const val = params[key];

        // check if val is a string[] and convert to json string array
        if (Array.isArray(val)) {
            searchParams.set(key, JSON.stringify(val));
            continue;
        }
        
        const queryValue = params[key]?.toString() || '';
        searchParams.set(key, queryValue);
    }
    return searchParams.toString();
}

export function getWindowLocationQueryStringAsValueObject(columns: ColumnsObject): ValuesObject {
    const searchParams = new URLSearchParams(window.location.search);
    const params: ValuesObject = {};
    for (const [key, value] of searchParams) {
        const col = columns[key];
        if (col) {
            params[key] = convertValueFromString(col, value);
        }
    }
    return params;
}