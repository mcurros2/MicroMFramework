import { useEffect, useState } from "react";
import { Value } from "../../client";
import { UseEntityFormReturnType } from "../Form";

export interface UseHierarchyKeysProps {
    formAPI: UseEntityFormReturnType,
    hierarchy: string[],
    mappedHierarchy?: string[]
}

const generateParentKeysArray = (hierarchy: string[], mappedHierarchy: string[] | undefined, values: Record<string, Value>, changedIndex?: number) => {
    return hierarchy.map((_, index) => {
        const parentKeys: Record<string, Value> = {};

        for (let i = 0; i <= index; i++) {
            const formValueName = hierarchy[i];
            const mappedName = mappedHierarchy?.[i] || formValueName;
            parentKeys[mappedName] = changedIndex !== undefined && i > changedIndex
                ? ''
                : values[formValueName];
        }

        return parentKeys;
    });
};

const areParentKeysArraysEqual = (left: Record<string, Value>[], right: Record<string, Value>[]) => {
    if (left.length !== right.length) return false;

    return left.every((leftKeys, index) => {
        const rightKeys = right[index];
        const leftNames = Object.keys(leftKeys);
        const rightNames = Object.keys(rightKeys);

        return leftNames.length === rightNames.length && leftNames.every(name => leftKeys[name] === rightKeys[name]);
    });
};

const areStringArraysEqual = (left: string[] | undefined, right: string[] | undefined) => {
    if (left === undefined || right === undefined) return left === right;
    return left.length === right.length && left.every((value, index) => value === right[index]);
};

interface HierarchyKeysState {
    parentKeysArray: Record<string, Value>[];
    hierarchyValues: Value[];
    hierarchy: string[];
    mappedHierarchy?: string[];
    processedGetStatus?: UseEntityFormReturnType['status'];
    changedIndex?: number;
    changeRevision: number;
}

export function useHierarchyKeys(props: UseHierarchyKeysProps) {
    const { formAPI, hierarchy, mappedHierarchy } = props;

    const [hierarchyState, setHierarchyState] = useState<HierarchyKeysState>(() => ({
        parentKeysArray: generateParentKeysArray(hierarchy, mappedHierarchy, formAPI.form.values),
        hierarchyValues: hierarchy.map(name => formAPI.form.values[name]),
        hierarchy: [...hierarchy],
        mappedHierarchy: mappedHierarchy ? [...mappedHierarchy] : undefined,
        changeRevision: 0,
    }));

    const currentHierarchyValues = hierarchy.map(name => formAPI.form.values[name]);

    const isGetLoading = formAPI.status.operationType === 'get' && formAPI.status.loading === true;

    const isExistingRecordMode = formAPI.formMode === 'edit' || formAPI.formMode === 'view';

    const isNewCompletedGet = isExistingRecordMode
        && formAPI.status.operationType === 'get'
        && formAPI.status.loading === false
        && hierarchyState.processedGetStatus !== formAPI.status;

    const hierarchyConfigurationChanged = !areStringArraysEqual(hierarchyState.hierarchy, hierarchy)
        || !areStringArraysEqual(hierarchyState.mappedHierarchy, mappedHierarchy);

    let detectedChangedIndex = -1;
    if (!isGetLoading && !isNewCompletedGet && !hierarchyConfigurationChanged) {
        // Use the deepest changed level so controls that update several valid hierarchy
        // levels together only invalidate values below the last value they supplied.
        currentHierarchyValues.forEach((value, index) => {
            if (hierarchyState.hierarchyValues[index] !== value) {
                detectedChangedIndex = index;
            }
        });
    }

    let resolvedHierarchyState = hierarchyState;

    if (!isGetLoading) {
        const changedIndex = isNewCompletedGet || hierarchyConfigurationChanged
            ? undefined
            : detectedChangedIndex === -1 ? hierarchyState.changedIndex : detectedChangedIndex;

        const newParentKeysArray = generateParentKeysArray(
            hierarchy,
            mappedHierarchy,
            formAPI.form.values,
            changedIndex
        );

        const parentKeysChanged = !areParentKeysArraysEqual(hierarchyState.parentKeysArray, newParentKeysArray);

        const hierarchyChanged = isNewCompletedGet || hierarchyConfigurationChanged || detectedChangedIndex !== -1;

        if (parentKeysChanged || hierarchyChanged) {
            // Reconcile before children render so callbacks and effects cannot capture
            // form values from this render with parent keys from the previous render.
            const newHierarchyState: HierarchyKeysState = {
                parentKeysArray: parentKeysChanged ? newParentKeysArray : hierarchyState.parentKeysArray,
                hierarchyValues: currentHierarchyValues,
                hierarchy: [...hierarchy],
                mappedHierarchy: mappedHierarchy ? [...mappedHierarchy] : undefined,
                processedGetStatus: isNewCompletedGet ? formAPI.status : hierarchyState.processedGetStatus,
                changedIndex,
                changeRevision: detectedChangedIndex === -1
                    ? hierarchyState.changeRevision
                    : hierarchyState.changeRevision + 1,
            };

            setHierarchyState(newHierarchyState);
            resolvedHierarchyState = newHierarchyState;
        }
    }

    const committedChangedIndex = hierarchyState.changedIndex;
    const committedChangeRevision = hierarchyState.changeRevision;
    const committedHierarchy = hierarchyState.hierarchy;

    useEffect(() => {
        if (committedChangedIndex === undefined) return;

        // Changing the bound value is handled by the control. Descendants must be
        // changed through the form controls receive the update.
        committedHierarchy.slice(committedChangedIndex + 1).forEach((name) => {
            formAPI.form.setFieldValue(name, '');
        });

    }, [committedChangedIndex, committedChangeRevision, committedHierarchy, formAPI.form]);

    return resolvedHierarchyState.parentKeysArray;
}
