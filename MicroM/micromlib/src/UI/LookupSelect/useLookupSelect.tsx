import { SelectItem } from "@mantine/core";
import { ReactNode, useEffect } from "react";
import { Value, ValuesObject } from "../../client";
import { Entity, EntityColumn, EntityDefinition } from "../../Entity";
import { useExecuteView, useLocaleFormat, useStateReturnType } from "../Core";
import { UseEntityFormReturnType } from "../Form";
import { useLookupEntity, useLookupForm } from "../Lookup";

export interface UseLookupSelectOptions {
    parentKeys?: ValuesObject,
    selectDataState: useStateReturnType<SelectItem[]>,
    triggerRefreshState: useStateReturnType<boolean>,
    column: EntityColumn<Value>,
    entityForm: UseEntityFormReturnType,
    entity: Entity<EntityDefinition>,
    lookupDefName: string,
    maxItems?: number,
    includeKeyInDescription?: boolean,
    breadCrumbs?: ReactNode,
}
export const useLookupSelect = (props: UseLookupSelectOptions) => {
    const { parentKeys, selectDataState, triggerRefreshState, column, entityForm,
        entity, lookupDefName, maxItems, includeKeyInDescription, breadCrumbs
    } = props;

    const [triggerRefresh, setTriggerRefresh] = triggerRefreshState;

    const localeFormat = useLocaleFormat({ timeZoneOffset: entity.API.client.TIMEZONE_OFFSET });

    const { lookupEntity, lookupDef, lookupViewName } = useLookupEntity({ entity, lookupDefName, parentKeys });

    const [selectData, setSelectData] = selectDataState;

    const openEditModal = useLookupForm();

    const handleEditOnClosed = () => {
        setTriggerRefresh((prev) => !prev);
    }

    const handleOnOK = (selectedKeys: ValuesObject[]) => {
        if (selectedKeys.length > 0) {
            const keys = selectedKeys[0];
            const key_index = lookupDef?.viewMapping?.keyIndex ?? 0;
            const value = keys[Object.keys(keys)[key_index]];
            entityForm.form.setFieldValue(column.name, value);
        }
    };

    const handleEditClick = () => {
        openEditModal({
            entity: lookupEntity!,
            enableAdd: true, enableEdit: true, enableDelete: true, enableView: true,
            parentKeys, viewName: lookupViewName, onClosed: handleEditOnClosed, onOK: handleOnOK, selectionMode: 'single', breadCrumbs: breadCrumbs
        })
    }

    const status = useExecuteView(lookupEntity, parentKeys, lookupViewName, undefined, maxItems?.toString(), triggerRefresh);
   
    // MMC: Effect for setting the select data
    useEffect(() => {
        if (status.loading) return;

        if (status.data && status.data[0].records) {
            const keyIndex = lookupDef?.viewMapping?.keyIndex ?? 0;
            const descIndex = lookupDef?.viewMapping?.descriptionIndex ?? 1;

            // MMC: for working with objects as expected data format
            const tableData = status.data[0].records.map((record) => {
                const formatedDescription = localeFormat.formatValue(record[descIndex], status.data![0].typeInfo[descIndex]);
                const formatedKey = localeFormat.formatValue(record[keyIndex], status.data![0].typeInfo[descIndex]);
                return { value: record[keyIndex]?.toString(), label: (includeKeyInDescription === true) ? `${formatedKey} - ${formatedDescription}` : formatedDescription } as SelectItem;
            });

            setSelectData(tableData);

        } else {
            setSelectData([]);
        }

    }, [includeKeyInDescription, localeFormat.locale, status.loading, status.data]);

    return {
        onEditClick: handleEditClick,
        status: status
    }

}