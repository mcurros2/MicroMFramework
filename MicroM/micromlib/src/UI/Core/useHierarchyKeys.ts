import { useEffect, useRef, useState } from "react";
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

export function useHierarchyKeys(props: UseHierarchyKeysProps) {
    const { formAPI, hierarchy, mappedHierarchy } = props;

    const [parentKeysArray, setParentKeysArray] = useState<Record<string, Value>[]>(() =>
        generateParentKeysArray(hierarchy, mappedHierarchy, formAPI.form.values)
    );
    const parentKeysArrayRef = useRef(parentKeysArray);

    // Previous values are handled in hierarchy order and are not affected by mapping.
    const previousHierarchyValues = useRef<Value[]>(hierarchy.map(name => formAPI.form.values[name]));
    const processedGetStatus = useRef<typeof formAPI.status | undefined>(undefined);

    useEffect(() => {
        const currentHierarchyValues = hierarchy.map(name => formAPI.form.values[name]);
        const isGetLoading = formAPI.status.operationType === 'get' && formAPI.status.loading === true;

        // Values applied by a get are authoritative. Wait for the get to finish, then
        // synchronize the hierarchy without treating those values as user changes.
        if (isGetLoading) return;

        const isExistingRecordMode = formAPI.formMode === 'edit' || formAPI.formMode === 'view';
        const isNewCompletedGet = isExistingRecordMode
            && formAPI.status.operationType === 'get'
            && formAPI.status.loading === false
            && processedGetStatus.current !== formAPI.status;

        if (isNewCompletedGet) {
            const fetchedParentKeysArray = generateParentKeysArray(hierarchy, mappedHierarchy, formAPI.form.values);

            if (!areParentKeysArraysEqual(parentKeysArrayRef.current, fetchedParentKeysArray)) {
                // eslint-disable-next-line react-hooks/set-state-in-effect
                setParentKeysArray(fetchedParentKeysArray);
                parentKeysArrayRef.current = fetchedParentKeysArray;
            }

            previousHierarchyValues.current = currentHierarchyValues;
            processedGetStatus.current = formAPI.status;
            return;
        }

        // Use the deepest changed level so controls that update several valid hierarchy
        // levels together only invalidate values below the last value they supplied.
        let changedIndex = -1;
        currentHierarchyValues.forEach((value, index) => {
            if (previousHierarchyValues.current[index] !== value) {
                changedIndex = index;
            }
        });

        if (changedIndex === -1) return;

        const newParentKeysArray = generateParentKeysArray(hierarchy, mappedHierarchy, formAPI.form.values, changedIndex);

        if (!areParentKeysArraysEqual(parentKeysArrayRef.current, newParentKeysArray)) {
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setParentKeysArray(newParentKeysArray);
            parentKeysArrayRef.current = newParentKeysArray;
        }

        previousHierarchyValues.current = currentHierarchyValues;

        // Changing the bound value is handled by the control. Descendants must be
        // changed through the form controls receive the update.
        hierarchy.slice(changedIndex + 1).forEach((name) => {
            formAPI.form.setFieldValue(name, '');
        });

    }, [formAPI.form, formAPI.form.values, formAPI.formMode, formAPI.status, hierarchy, mappedHierarchy]);

    return parentKeysArray;
}
