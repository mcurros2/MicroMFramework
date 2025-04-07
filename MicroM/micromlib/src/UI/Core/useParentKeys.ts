import { useEffect, useRef, useState } from "react";
import { Entity, EntityDefinition, areValuesObjectsEqual, copyValuesObject } from "../../Entity";
import { ValuesObject } from "../../client";
import { UseEntityFormReturnType } from "../Form";

export interface UseParentKeysProps {
    formAPI: UseEntityFormReturnType;
    columnNames: string[];
    entity: Entity<EntityDefinition>;
}

const createParentKeysObject = (columnNames: string[], values: ValuesObject, entity: Entity<EntityDefinition>) => {
    const parentKeys: ValuesObject = {};

    const formKeys = Object.keys(values);
    const entityKeys = Object.keys(entity.def.columns);
    const entityParentKeysNames = entity.parentKeys ? Object.keys(entity.parentKeys) : [];

    for (let i = 0; i < columnNames.length; i++) {
        const colName = columnNames[i];
        // MMC: if the column is in the form, use the value from the form
        if (formKeys.includes(colName)) {
            parentKeys[colName] = values[colName];
        }
        // MMC: if the column is in the entity, use the value from the entity
        else if (entityKeys.includes(colName)) {
            parentKeys[colName] = entity.def.columns[colName].value;
        }
        // MMC: if the column is in the parentKeys, use the value from the parentKeys
        else if (entityParentKeysNames.includes(colName)) {
            parentKeys[colName] = entity.parentKeys[colName];
        }
        // MMC: otherwise, set the value to ''
        else {
            parentKeys[colName] = '';
        }
    }

    return parentKeys;
}

export function useParentKeys(props: UseParentKeysProps) {
    const { formAPI, columnNames, entity } = props;

    const [parentKeys, setParentKeys] = useState<ValuesObject>(createParentKeysObject(columnNames, formAPI.form.values, entity));
    const previousParentKeys = useRef({});


    useEffect(() => {
        const newParentKeys = createParentKeysObject(columnNames, formAPI.form.values, entity);
        if (areValuesObjectsEqual(previousParentKeys.current, newParentKeys)) return;

        setParentKeys(newParentKeys);
        previousParentKeys.current = copyValuesObject(newParentKeys);

    }, [formAPI.form, formAPI.form.values, columnNames, entity, entity.parentKeys]);

    return parentKeys;
}
