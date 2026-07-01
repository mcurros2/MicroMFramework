import { ActionIcon, Button, Group, Loader, Select, SelectItem, SelectProps, useComponentDefaultProps, useMantineTheme } from "@mantine/core"
import { IconSelector } from "@tabler/icons-react"
import { forwardRef, ReactNode, useState } from "react"
import { DataResult, DBStatusResult, OperationStatus, Value, ValuesObject } from "../../client"
import { Entity, EntityColumn, EntityColumnFlags, EntityDefinition } from "../../Entity"
import { ActionIconVariant, ButtonVariant, MicroMWidthSizes } from "../Core"
import { UseEntityFormReturnType, useFieldConfiguration } from "../Form"
import { useLookupSelect } from "./useLookupSelect"
import { useLookupSelectInputProps } from "./useLookupSelectInputProps"


export interface CustomSelectProps extends Omit<SelectProps, 'data'> { }

export interface LookupSelectOptions {
    parentKeys?: ValuesObject,
    column: EntityColumn<Value>,
    formStatus?: OperationStatus<DBStatusResult | DataResult | ValuesObject>,
    entityForm: UseEntityFormReturnType
    entity: Entity<EntityDefinition>,
    lookupDefName: string,
    enableEdit?: boolean,
    selectProps?: Omit<CustomSelectProps, 'autoFocus'>,
    editIcon?: React.ReactNode,
    editIconVariant?: ActionIconVariant,
    maxItems?: number,
    requiredLabel?: string,
    editLabel?: string,
    includeKeyInDescription?: boolean,
    withinPortal?: boolean,
    zIndex?: number,
    editButtonVariant?: ButtonVariant,
    breadCrumbs?: ReactNode,
    maxWidth?: keyof typeof MicroMWidthSizes,
    minWidth?: keyof typeof MicroMWidthSizes,
    autoFocus?: 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean,
}

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
    minWidth: "sm",
    selectProps: {
        searchable: true,
        maxDropdownHeight: 260,
        clearable: true,
    }
}

export const LookupSelect = forwardRef<HTMLInputElement, LookupSelectOptions>(function LookupSelect(props: LookupSelectOptions, ref) {
    const {
        parentKeys, column, formStatus,
        entityForm, entity, lookupDefName, enableEdit,
        editIcon, editIconVariant, selectProps, maxItems,
        requiredLabel, editLabel, includeKeyInDescription,
        withinPortal, zIndex, breadCrumbs, maxWidth, minWidth, autoFocus
    } = useComponentDefaultProps('LookupSelect', LookupSelectDefaultProps, props);

    const theme = useMantineTheme();

    const { formMode, form } = entityForm;

    const triggerRefreshState = useState<boolean>(true);
    const selectDataState = useState<SelectItem[]>([]);
    const [selectData] = selectDataState;

    const lookupSelectAPI = useLookupSelect({ parentKeys, selectDataState, triggerRefreshState, column, entityForm, entity, lookupDefName, maxItems, includeKeyInDescription, breadCrumbs });

    const inputProps = useLookupSelectInputProps({ entityForm, column, selectData });

    const [showDescription,] = entityForm.showDescriptionState;

    const add_autofocus = formMode === 'add' ? true : undefined;
    const edit_autofocus = formStatus?.loading === false && formMode !== 'add' ? true : undefined;

    const readonly_condition = selectProps?.readOnly === undefined ? (entityForm.formMode !== 'add' && column.hasFlag(EntityColumnFlags.pk)) : selectProps.readOnly;
    const autofocus_condition = autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus;

    const resolvedSelectProps: CustomSelectProps = {
        ...(selectProps ?? {}),
        readOnly: readonly_condition,
        miw: selectProps?.miw ?? (minWidth !== 'auto' && minWidth !== undefined) ? MicroMWidthSizes[minWidth!] : undefined,
        maw: selectProps?.maw ?? (maxWidth !== 'auto' && maxWidth !== undefined) ? MicroMWidthSizes[maxWidth!] : undefined,
        label: selectProps?.label ?? column.prompt,
        autoFocus: autofocus_condition,
        description: showDescription ? (selectProps?.description ?? column.description) : ''
    };

    useFieldConfiguration({ entityForm, column, required: resolvedSelectProps?.required, requiredMessage: requiredLabel });

    const readoOnlyResult = resolvedSelectProps?.readOnly || entityForm.formMode === 'view' || lookupSelectAPI.status.loading || formStatus?.loading || (column.hasFlag(EntityColumnFlags.pk) && entityForm.formMode !== 'add') ? true : false;

    return (
        <Select
            {...resolvedSelectProps}
            data-autofocus={autofocus_condition}
            withAsterisk={resolvedSelectProps.withAsterisk ?? (!resolvedSelectProps.readOnly && !(entityForm.formMode === 'view') && (resolvedSelectProps.required ?? !column.hasFlag(EntityColumnFlags.nullable)))}
            withinPortal={withinPortal}
            zIndex={zIndex}
            icon={lookupSelectAPI.status.loading ? <Loader size="xs" /> : resolvedSelectProps.icon}
            data={selectData}
            readOnly={readoOnlyResult}
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
            {...inputProps}
            ref={ref}
        />
    )
});
