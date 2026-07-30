import { Value, ValuesObject } from "../client/client.types";


export const isScalar = (value: unknown) =>
    value === null || ['string', 'number', 'boolean'].includes(typeof value);

export const isValuesObject = (value: unknown): value is ValuesObject => {
    if (!value || typeof value !== 'object' || Array.isArray(value)) return false;
    return Object.values(value).every(isScalar);
};

export function hasValue(value: Value) {
    return value !== '' && value !== null && typeof value !== 'undefined';
}

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

export function mergeValuesObject(source?: ValuesObject, mergeAndOverride?: ValuesObject) {
    const mergedValues: ValuesObject = {};

    Object.entries(source ?? {}).forEach(([name, value]) => {
        if (hasValue(value)) mergedValues[name] = value;
    });

    Object.entries(mergeAndOverride ?? {}).forEach(([name, value]) => {
        if (hasValue(value) || !Object.hasOwnProperty.call(mergedValues, name)) {
            mergedValues[name] = value;
        }
    });

    return mergedValues;
};

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

