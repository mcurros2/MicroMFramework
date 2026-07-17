import { Value, ValuesObject } from "../client";
import { CompoundKeyGroup, DefaultKeySeparator, EntityView } from "./EntityView";

export const MaxCompoundKeyValues = 10;

export type OrderedCompoundKeyMapping = readonly [columnName: string, position: number];

export function getOrderedCompoundKeyMappings(group: CompoundKeyGroup): OrderedCompoundKeyMapping[] {
    const mappings = Object.entries(group.keyMappings).sort((left, right) => left[1] - right[1]);
    if (mappings.length < 2 || mappings.length > MaxCompoundKeyValues || mappings.some(([, position], index) => position !== index)) {
        throw new Error(`Compound key groups must define ${2}-${MaxCompoundKeyValues} unique, contiguous positions beginning at zero.`);
    }
    return mappings;
}

export function resolveCompoundKeyGroup(view: EntityView, groupName: string): CompoundKeyGroup {
    const group = view.compoundKeyGroups?.[groupName];
    if (!group) throw new Error(`Compound key group '${groupName}' was not found in view '${view.name}'.`);
    getOrderedCompoundKeyMappings(group);
    return group;
}

export function splitCompoundKey(rawValue: string, group: CompoundKeyGroup) {
    const expectedCount = getOrderedCompoundKeyMappings(group).length;
    const values = rawValue.split(group.keySeparator ?? DefaultKeySeparator);
    return {
        values,
        complete: values.length === expectedCount && values.every(value => value.length > 0),
    };
}

export function composeCompoundKey(values: readonly Value[], group: CompoundKeyGroup): string {
    getOrderedCompoundKeyMappings(group);
    const parts = values.map(value => value?.toString() ?? '');
    return parts.every(value => value.length === 0) ? '' : parts.join(group.keySeparator ?? DefaultKeySeparator);
}

export function extractCompoundKeyValues(keys: ValuesObject, group: CompoundKeyGroup): Value[] | undefined {
    const values = getOrderedCompoundKeyMappings(group).map(([columnName]) => keys[columnName]);
    return values.every(value => value !== undefined && value !== null && value.toString().length > 0) ? values : undefined;
}

export function extractCompoundRecordKeys(record: object, group: CompoundKeyGroup): ValuesObject | undefined {
    const properties = Object.keys(record);
    if (group.viewIndex < 0 || group.viewIndex >= properties.length) return undefined;
    const rawValue = (record as Record<string, unknown>)[properties[group.viewIndex]];
    if (typeof rawValue !== 'string') return undefined;
    const split = splitCompoundKey(rawValue, group);
    if (!split.complete) return undefined;

    const result: ValuesObject = {};
    getOrderedCompoundKeyMappings(group).forEach(([columnName, position]) => result[columnName] = split.values[position]);
    return result;
}
