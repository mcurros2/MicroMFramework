import { useEffect, useRef, useState } from "react";
import { Value } from "../../client";
import { UseEntityFormReturnType } from "../Form";

export interface UseHierarchyKeysProps {
    formAPI: UseEntityFormReturnType,
    hierarchy: string[],
    mappedHierarchy?: string[]
}

const generateInitialParentKeysArray = (hierarchy: string[]) => {
    const initialArray: Record<string, Value>[] = [];
    for (let i = 0; i < hierarchy.length; i++) {
        const parentKeys: Record<string, Value> = {};
        for (let j = 0; j < i; j++) {
            parentKeys[hierarchy[j]] = '';
        }
        initialArray.push(parentKeys);
    }
    return initialArray;
};

export function useHierarchyKeys(props: UseHierarchyKeysProps) {
    const { formAPI, hierarchy, mappedHierarchy } = props;

    const [parentKeysArray, setParentKeysArray] = useState<Record<string, Value>[]>(() => generateInitialParentKeysArray(mappedHierarchy || hierarchy));
    const parentKeysArrayRef = useRef(parentKeysArray);

    // Previous values are handled in an array, not affected by mapping
    // Need to make a copy here cause mantine do shallow merge and retain the same reference
    const previousHierarchyValues = useRef<Value[]>(JSON.parse(JSON.stringify(formAPI.form.values)));

    useEffect(() => {
        // Check if hierarchy values had changed
        let hasHierarchyValueChanged = false;
        let changedIndex = -1;
        hierarchy.forEach((h, index) => {
            if (previousHierarchyValues.current[index] !== formAPI.form.values[h]) {
                hasHierarchyValueChanged = true;
                changedIndex = index;
            }
        });

        // If no hierarchy value has changed, exit the effect
        if (!hasHierarchyValueChanged) return;

        const newParentKeysArray: Record<string, Value>[] = [];

        hierarchy.forEach((name, index) => {
            const parentKeys: Record<string, Value> = {};
            for (let i = 0; i <= index; i++) {
                const formValueName = hierarchy[i];
                const mappedName = mappedHierarchy?.[i] || formValueName;

                // If we are looking at a level subsequent to the changed level, reset its value.
                if (i > changedIndex) {
                    parentKeys[mappedName] = '';
                } else if (formValueName in formAPI.form.values) {
                    parentKeys[mappedName] = formAPI.form.values[formValueName];
                }
            }

            newParentKeysArray.push(parentKeys);
        });

        const hasChanged = newParentKeysArray.some((item, index) => {
            const oldKeys = parentKeysArrayRef.current[index];
            const newKeys = item;
            return Object.keys(newKeys).some(key => newKeys[key] !== oldKeys[key]);
        });

        if (hasChanged) {
            //console.log('Parent keys array', newParentKeysArray);
            setParentKeysArray(newParentKeysArray);
            parentKeysArrayRef.current = newParentKeysArray;

            // MMC: as we are changing related values due to a parent change we need to call setFieldValue
            // changing the binded value is taken care by default
            hierarchy.slice(changedIndex + 1).forEach((name) => {
                formAPI.form.setFieldValue(name, '');
            });
        }

        previousHierarchyValues.current = hierarchy.map(h => formAPI.form.values[h]);

    }, [formAPI.form, formAPI.form.values, hierarchy, mappedHierarchy]);

    return parentKeysArray;
}
