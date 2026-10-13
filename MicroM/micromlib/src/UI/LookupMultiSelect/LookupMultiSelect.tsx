import { Group, GroupProps, Loader, MultiSelect, MultiSelectProps, SelectItem, useComponentDefaultProps, useMantineTheme } from "@mantine/core"
import { useCallback, useEffect, useMemo, useState } from "react"
import { DataResult, DBStatusResult, OperationStatus, Value, ValuesObject } from "../../client"
import { Entity, EntityColumn, EntityColumnFlags, EntityDefinition } from "../../Entity"
import { FieldAutoFocus, resolveFieldAutoFocus, resolveFieldReadOnly, resolveFieldWidths } from "../Core/fieldUIHelpers"
import { MicroMWidthSizes } from "../Core/types"
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form"
import { useLookupSelect } from "../LookupSelect"
import { useLookupMultiSelectInputProps } from "./useLookupMultiSelectInputProps"


export interface LookupMultiSelectProps extends Omit<MultiSelectProps, 'data' | 'autoFocus'> {
    autoFocus?: FieldAutoFocus,
    parentKeys?: ValuesObject,
    column: EntityColumn<Value>,
    formStatus?: OperationStatus<DBStatusResult | DataResult | ValuesObject>,
    entityForm: UseEntityFormReturnType
    entity: Entity<EntityDefinition>,
    lookupDefName: string,
    requiredLabel?: string,
    includeKeyInDescription?: boolean,
    createLabel?: string,
    containerProps?: GroupProps
    grow?: boolean,
    maxWidth?: keyof typeof MicroMWidthSizes,
    minWidth?: keyof typeof MicroMWidthSizes,
}


export const LookupMultiSelectDefaultProps: Partial<LookupMultiSelectProps> = {
    requiredLabel: "A value from the list is required",
    includeKeyInDescription: true,
    searchable: true,
    maxDropdownHeight: 260,
    clearable: true,
    createLabel: "+ Create",
    withinPortal: true,
    zIndex: 100000,
    maxWidth: "sm",
}

export function LookupMultiSelect(props: LookupMultiSelectProps) {
    const {
        parentKeys, column, formStatus,
        entityForm, entity, lookupDefName,
        requiredLabel, includeKeyInDescription, label, description, required,
        icon, readOnly, searchable, maxDropdownHeight, clearable, containerProps,
        creatable, createLabel, withAsterisk, grow, maxWidth, minWidth, miw, maw, autoFocus, ...rest
    } = useComponentDefaultProps('LookupMultiSelect', LookupMultiSelectDefaultProps, props);

    const containerPropsMemo = useMemo(() => {
        if (grow) {
            return { ...containerProps, style: { ...containerProps?.style, flex: 'auto' } };
        }
        return containerProps;
    }, [containerProps, grow])

    const theme = useMantineTheme();

    const triggerRefreshState = useState<boolean>(true);
    const selectDataState = useState<SelectItem[]>([]);
    const [selectData, setSelectData] = selectDataState;

    const lookupSelectAPI = useLookupSelect({ parentKeys, selectDataState, triggerRefreshState, column, entityForm, entity, lookupDefName, maxItems: 0, includeKeyInDescription });

    const inputProps = useLookupMultiSelectInputProps({ entityForm, column, selectData });

    const [showDescription,] = entityForm.showDescriptionState;

    useFieldConfiguration({ entityForm, column, required: required, requiredMessage: requiredLabel });

    const onCreate = (newValue: string) => {
        const item = { value: newValue, label: newValue };
        setSelectData((current) => [...current, item]);
        return item;
    }

    const updateDescription = useCallback((value: Value) => {
        // Get the seted values description. Value is an array of values
        if (column.isArray) {
            // MMC: in rare cases the column can have a value but there is no selectData
            //  in that case skip updating column description 
            //(I found this placing a lookupMultiSelect inside an accordion item, where it mounts and unmounts the component before the data is fetched)
            if (column.value && selectData.length === 0) return;

            const values = (value === null || value === undefined || value === '' ? [] : value as string[]);
            const descriptions = values.map((value) => {
                const index = selectData.findIndex((item) => item.value === value);
                return index >= 0 ? selectData[index].label : '';
            });
            column.valueDescription = descriptions.join(', ');
        }
    }, [column, selectData]);

    // MMC: Effect for setting the column valueDescription
    const selectedValue = entityForm.form.values[column.name];
    useEffect(() => {
        updateDescription(selectedValue);
    }, [selectedValue, updateDescription]);

    const { miw: effective_miw, maw: effective_maw } = resolveFieldWidths({ miw, maw, minWidth, maxWidth });
    const resolvedAutoFocus = resolveFieldAutoFocus(autoFocus, entityForm.formMode, entityForm.status.loading);
    const effectiveReadOnly = resolveFieldReadOnly(column, entityForm.formMode, readOnly) || Boolean(lookupSelectAPI.status.loading || formStatus?.loading);

    return (
        <Group {...containerPropsMemo} miw={effective_miw} maw={effective_maw}>
            <MultiSelect
                {...rest}
                withAsterisk={withAsterisk ?? (!readOnly && !(entityForm.formMode === 'view') && (required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
                styles={{ dropdown: { backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[4] : theme.colors.gray[3] }, root: { flexGrow: 1 } }}
                searchable={searchable}
                maxDropdownHeight={maxDropdownHeight}
                clearable={clearable}
                label={label ?? column.prompt}
                description={showDescription ? (description ?? column.description) : ''}
                icon={lookupSelectAPI.status.loading ? <Loader size="xs" /> : icon}
                data={selectData}
                error={lookupSelectAPI.status.error ? lookupSelectAPI.status.error.message : null}
                creatable={!effectiveReadOnly && creatable}
                onCreate={!effectiveReadOnly && creatable ? onCreate : undefined}
                getCreateLabel={(newValue) => `${createLabel} ${newValue}`}
                {...inputProps}
                readOnly={effectiveReadOnly}
                autoFocus={resolvedAutoFocus}
                data-autofocus={resolvedAutoFocus ? true : undefined}
                miw={effective_miw}
                maw={effective_maw}
            />
        </Group>
    )
}
