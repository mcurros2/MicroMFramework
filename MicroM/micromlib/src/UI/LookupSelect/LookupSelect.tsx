import { ActionIcon, Button, Group, Loader, Select, SelectItem, SelectProps, useComponentDefaultProps, useMantineTheme } from "@mantine/core"
import { IconSelector } from "@tabler/icons-react"
import { forwardRef, useEffect, useState } from "react"
import { Entity, EntityColumn, EntityColumnFlags, EntityDefinition } from "../../Entity"
import { DBStatusResult, DataResult, OperationStatus, Value, ValuesObject } from "../../client"
import { ActionIconVariant, ButtonVariant } from "../Core"
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form"
import { useLookupSelect } from "./useLookupSelect"


export interface CustomSelectProps extends Omit<SelectProps, 'data'> { }

export interface LookupSelectOptions {
    parentKeys?: ValuesObject,
    column: EntityColumn<Value>,
    formStatus?: OperationStatus<DBStatusResult | DataResult | ValuesObject>,
    entityForm: UseEntityFormReturnType
    entity: Entity<EntityDefinition>,
    lookupDefName: string,
    enableEdit?: boolean,
    selectProps?: CustomSelectProps,
    editIcon?: React.ReactNode,
    editIconVariant?: ActionIconVariant,
    maxItems?: number,
    requiredLabel?: string,
    editLabel?: string,
    includeKeyInDescription?: boolean,
    withinPortal?: boolean,
    zIndex?: number,
    editButtonVariant?: ButtonVariant
}

/*
    editIcon: <IconPencil size="1rem" stroke="1.5" />,
    editIconVariant: "light",

*/


export const LookupSelectDefaultProps: Partial<LookupSelectOptions> = {
    enableEdit: false,
    maxItems: 0,
    requiredLabel: "A value from the list is required",
    editLabel: "Edit",
    includeKeyInDescription: false,
    withinPortal: true,
    zIndex: 100000,
    editButtonVariant: "light",
    editIconVariant: "light",
    selectProps: {
        searchable: true,
        maxDropdownHeight: 260,
        clearable: true,
        //w: "40rem",
    }
}

export const LookupSelect = forwardRef<HTMLInputElement, LookupSelectOptions>(function LookupSelect(props: LookupSelectOptions, ref) {
    const {
        parentKeys, column, formStatus,
        entityForm, entity, lookupDefName, enableEdit,
        editIcon, editIconVariant, selectProps, maxItems,
        requiredLabel, editLabel, includeKeyInDescription,
        withinPortal, zIndex
    } = useComponentDefaultProps('LookupSelect', LookupSelectDefaultProps, props);

    const theme = useMantineTheme();

    const triggerRefreshState = useState<boolean>(true);
    const selectDataState = useState<SelectItem[]>([]);
    const [selectData] = selectDataState;

    const lookupSelectAPI = useLookupSelect({ parentKeys, selectDataState, triggerRefreshState, column, entityForm, entity, lookupDefName, maxItems, includeKeyInDescription });

    const [showDescription,] = entityForm.showDescriptionState;

    selectProps!.label = selectProps?.label ?? column.prompt;
    selectProps!.description = showDescription ? (selectProps?.description ?? column.description) : '';

    useFieldConfiguration({ entityForm, column, required: selectProps?.required, requiredMessage: requiredLabel });

    // MMC: Effect for setting the column valueDescription
    useEffect(() => {
        const value = entityForm.form.values[column.name];
        if (value) {
            const index = selectData.findIndex((item) => item.value === value);
            if (index >= 0) {
                column.valueDescription = selectData[index].label;
            }
            else {
                column.valueDescription = '';
            }
        }
        else {
            column.valueDescription = '';
        }
    }, [column, entityForm.form.values, selectData]);


    return (
        <Select
            {...selectProps}
            withAsterisk={selectProps!.withAsterisk ?? (!selectProps!.readOnly && !(entityForm.formMode === 'view') && (selectProps!.required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            withinPortal={withinPortal}
            zIndex={zIndex}
            icon={lookupSelectAPI.status.loading ? <Loader size="xs" /> : selectProps?.icon}
            data={selectData}
            readOnly={selectProps?.readOnly || entityForm.formMode === 'view' || lookupSelectAPI.status.loading || formStatus?.loading ? true : false}
            error={lookupSelectAPI.status.error ? lookupSelectAPI.status.error.message : null}
            // MMC: this is how we hack the styles let the chevron work showing the list and the edit button be clickable
            styles={() => ({
                rightSection: { pointerEvents: (editIcon ? "none" : "all") }
            })}
            rightSection={enableEdit &&
                <Group spacing="xs">
                    <IconSelector size="1rem" />
                    {editIcon &&
                        <ActionIcon style={{ pointerEvents: 'all' }} size="md" color={theme.primaryColor} variant={editIconVariant} onClick={lookupSelectAPI.onEditClick}>
                            {editIcon}
                        </ActionIcon>
                    }
                    {!editIcon &&
                        <Button size="xs" mr="xs" variant="light" onClick={lookupSelectAPI.onEditClick}>{editLabel}</Button>
                    }
                </Group>
            }
            rightSectionWidth={(enableEdit) ? "auto" : undefined}
            {...lookupSelectAPI.inputProps}
            ref={ref}
        />
    )
});
