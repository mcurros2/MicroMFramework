import { ActionIcon, getSize, Group, Loader, MantineSize, rem, Stack, Text, TextInput, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { isNotEmpty } from "@mantine/form";
import { IconSearch } from "@tabler/icons-react";
import { useEffect, useRef } from "react";
import { Value, ValuesObject } from "../../client";
import { Entity, EntityColumn, EntityColumnFlags, EntityDefinition } from "../../Entity";
import { ActionIconVariant, useTextTransform, useTextTransformProps } from "../Core";
import { UseEntityFormReturnType } from "../Form";
import { LookupResultState, useLookup } from "../Lookup";

export interface LookupProps extends Omit<useTextTransformProps, 'entityForm' | 'column'> {
    parentKeys?: ValuesObject,
    column: EntityColumn<Value>,
    autoFocus?: 'autoFocusOnAdd' | 'autoFocusOnEdit' | boolean
    entityForm: UseEntityFormReturnType
    entity: Entity<EntityDefinition>,
    lookupDefName: string,
    required?: boolean,
    readonly?: boolean,
    disabled?: boolean,
    label?: string,
    idMaxWidth?: string,
    icon?: React.ReactNode,
    iconVariant?: ActionIconVariant,
    requiredLabel?: string
    description?: string,
    size?: MantineSize
    onLookupPerformed?: (lookupResult: LookupResultState) => void,
    enableAdd?: boolean,
    enableEdit?: boolean,
    enableDelete?: boolean,
    enableView?: boolean,
}

export const LookupDefaultProps: Partial<LookupProps> = {
    idMaxWidth: "15rem",
    icon: <IconSearch size="1rem" stroke="1.5" />,
    iconVariant: "light",
    requiredLabel: "A value is required",
    size: "sm",
    enableAdd: false,
    enableEdit: false,
    enableDelete: false,
    enableView: true,
    autoTrim: true,
}

export function Lookup(props: LookupProps) {
    const {
        entityForm, entity, lookupDefName, autoFocus, label, parentKeys, column, required, readonly, disabled, idMaxWidth, icon, iconVariant,
        requiredLabel, description, size, onLookupPerformed, enableAdd, enableEdit, enableDelete, enableView, transform, autoTrim
    } = useComponentDefaultProps('Lookup', LookupDefaultProps, props);


    const theme = useMantineTheme();
    const HTMLDescriptionRef = useRef(null);

    const textTransform = useTextTransform({ entityForm, column, transform, autoTrim });

    const lookupAPI = useLookup({
        entityForm: entityForm,
        entity: entity,
        lookupDefName: lookupDefName,
        column: column.name,
        parentKeys: parentKeys,
        required: required,
        HTMLDescriptionRef: HTMLDescriptionRef,
        enableAdd: enableAdd,
        enableEdit: enableEdit,
        enableDelete: enableDelete,
        enableView: enableView,
        transform: textTransform,
    });

    useEffect(() => {
        if (required ?? !column.hasFlag(EntityColumnFlags.nullable)) {
            entityForm.configureField(column, isNotEmpty(requiredLabel));
        }
        else {
            entityForm.removeValidation(column);
        }
    }, [column, entityForm, required, requiredLabel]);

    const controlSize = getSize({ size: size ?? "sm", sizes: theme.fontSizes });
    const descriptionSize = getSize({ size: size ?? "sm", sizes: theme.fontSizes });
    const labelColor = theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[9];
    const descriptionColor = theme.colorScheme === 'dark' ? theme.colors.dark[2] : theme.colors.gray[6];

    // MMC: set the binding column description value
    useEffect(() => {
        column.valueDescription = lookupAPI.lookupResult?.description;
    }, [column, lookupAPI.lookupResult?.description]);

    useEffect(() => {
        if (onLookupPerformed && lookupAPI.lookupResult) {
            onLookupPerformed(lookupAPI.lookupResult);
        }
    }, [lookupAPI.lookupResult, onLookupPerformed])

    const { formMode, status } = entityForm;
    const add_autofocus = formMode === 'add' ? true : undefined;
    const edit_autofocus = status.loading === false && formMode !== 'add' ? true : undefined;

    const readonly_condition = readonly === undefined ? column.hasFlag(EntityColumnFlags.autoNum) || (entityForm.formMode !== 'add' && column.hasFlag(EntityColumnFlags.pk)) : readonly;

    return (
        <Stack style={{ gap: "0.1rem", flexGrow: "1" }}>
            <Group style={{ gap: "0.2rem" }}>
                <Text size={controlSize} weight="500" color={labelColor}>{label ?? column.prompt}</Text>
                {(required ?? (!readonly && !(entityForm.formMode === 'view') && !column.hasFlag(EntityColumnFlags.nullable))) && <Text size={controlSize} weight="500" color={theme.colors.red[5]}>*</Text>}
            </Group>
            {(description ?? column.description) &&
                <Text style={{ fontSize: `calc(${descriptionSize} - ${rem(2)})`, lineHeight: 1.2 }} color={descriptionColor}>{description ?? column.description}</Text>
            }
            <Group style={{ marginTop: `calc(${theme.spacing.xs} / 2)` }} align="flex-start">
                <TextInput
                    size={size}
                    maw={idMaxWidth}
                    readOnly={readonly_condition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading ? true : false}
                    autoFocus={autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus}
                    data-autofocus={autoFocus === 'autoFocusOnAdd' ? add_autofocus : autoFocus === 'autoFocusOnEdit' ? edit_autofocus : autoFocus}
                    disabled={disabled}
                    key={`${entity.name}${column.name}`}
                    rightSection={<ActionIcon
                        color={theme.primaryColor}
                        disabled={readonly_condition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading}
                        onClick={async () => {
                            // readonly, no lookup
                            if (readonly_condition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading) return;

                            await lookupAPI.onBlur(column.name, true)
                        }}
                        variant={iconVariant}
                        size="md">{icon}
                    </ActionIcon>}
                    {...lookupAPI.lookupInputProps}
                    onBlur={async (e) => { await lookupAPI.onBlur(column.name, false, e) }}
                />
                <Group style={{ flexGrow: 1 }}>
                    <TextInput
                        size={size}
                        readOnly
                        key={`${entity.name}${column.name}description`}
                        value={lookupAPI.lookupResult?.description}
                        rightSection={lookupAPI.status.loading && <Loader size="xs" variant="bars" />}
                        ref={HTMLDescriptionRef}
                        sx={{ flexGrow: 1 }}
                    />
                </Group>
            </Group>
            {lookupAPI.lookupResult?.error &&
                <Group>
                    <Text size="xs" color="red">{lookupAPI.lookupResult.errorDescription}</Text>
                </Group>
            }
        </Stack>

    )
}