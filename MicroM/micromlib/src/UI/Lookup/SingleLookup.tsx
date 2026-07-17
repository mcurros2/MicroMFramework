import { ActionIcon, getSize, Group, Loader, rem, Stack, Text, TextInput, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { isNotEmpty } from "@mantine/form";
import { useEffect, useRef } from "react";
import { Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { useTextTransform } from "../Core";
import { LookupCommonProps, LookupDefaultProps } from "./Lookup.shared";
import { useLookup } from "./useLookup";

type SingleLookupProps = LookupCommonProps & { column: EntityColumn<Value>, bindingColumns?: never };

export function SingleLookup(props: SingleLookupProps) {
    const {
        entityForm, entity, lookupDefName, autoFocus, label, parentKeys, column, required, readonly, disabled, idMaxWidth, icon, iconVariant,
        requiredLabel, description, size, onLookupPerformed, enableAdd, enableEdit, enableDelete, enableView, transform, autoTrim
    } = useComponentDefaultProps('Lookup', LookupDefaultProps, props);

    const theme = useMantineTheme();
    const HTMLDescriptionRef = useRef(null);
    const textTransform = useTextTransform({ entityForm, column, transform, autoTrim });
    const lookupAPI = useLookup({
        entityForm, entity, lookupDefName, column: column.name, parentKeys, required, HTMLDescriptionRef,
        enableAdd, enableEdit, enableDelete, enableView, transform: textTransform,
    });

    useEffect(() => {
        if (required ?? !column.hasFlag(EntityColumnFlags.nullable)) entityForm.configureField(column, isNotEmpty(requiredLabel));
        else entityForm.removeValidation(column);
    }, [column, entityForm, required, requiredLabel]);

    const controlSize = getSize({ size: size ?? "sm", sizes: theme.fontSizes });
    const descriptionSize = getSize({ size: size ?? "sm", sizes: theme.fontSizes });
    const labelColor = theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[9];
    const descriptionColor = theme.colorScheme === 'dark' ? theme.colors.dark[2] : theme.colors.gray[6];

    useEffect(() => { column.valueDescription = lookupAPI.lookupResult?.description; }, [column, lookupAPI.lookupResult?.description]);
    useEffect(() => { if (onLookupPerformed && lookupAPI.lookupResult) onLookupPerformed(lookupAPI.lookupResult); }, [lookupAPI.lookupResult, onLookupPerformed]);

    const { formMode, status } = entityForm;
    const addAutofocus = formMode === 'add' ? true : undefined;
    const editAutofocus = status.loading === false && formMode !== 'add' ? true : undefined;
    const readonlyCondition = readonly === undefined ? column.hasFlag(EntityColumnFlags.autoNum) || (entityForm.formMode !== 'add' && column.hasFlag(EntityColumnFlags.pk)) : readonly;

    return (
        <Stack style={{ gap: "0.1rem", flexGrow: "1" }}>
            <Group style={{ gap: "0.2rem" }}>
                <Text size={controlSize} weight="500" color={labelColor}>{label ?? column.prompt}</Text>
                {(required ?? (!readonly && entityForm.formMode !== 'view' && !column.hasFlag(EntityColumnFlags.nullable))) && <Text size={controlSize} weight="500" color={theme.colors.red[5]}>*</Text>}
            </Group>
            {(description ?? column.description) && <Text style={{ fontSize: `calc(${descriptionSize} - ${rem(2)})`, lineHeight: 1.2 }} color={descriptionColor}>{description ?? column.description}</Text>}
            <Group style={{ marginTop: `calc(${theme.spacing.xs} / 2)` }} align="flex-start">
                <TextInput
                    size={size} maw={idMaxWidth}
                    readOnly={readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading}
                    autoFocus={autoFocus === 'autoFocusOnAdd' ? addAutofocus : autoFocus === 'autoFocusOnEdit' ? editAutofocus : autoFocus}
                    data-autofocus={autoFocus === 'autoFocusOnAdd' ? addAutofocus : autoFocus === 'autoFocusOnEdit' ? editAutofocus : autoFocus}
                    disabled={disabled} key={`${entity.name}${column.name}`}
                    rightSection={<ActionIcon color={theme.primaryColor}
                        disabled={readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading}
                        onClick={async () => {
                            if (readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading) return;
                            await lookupAPI.onBlur(column.name, true);
                        }}
                        variant={iconVariant} size="md">{icon}</ActionIcon>}
                    {...lookupAPI.lookupInputProps}
                    onBlur={async event => { await lookupAPI.onBlur(column.name, false, event); }}
                />
                <Group style={{ flexGrow: 1 }}>
                    <TextInput size={size} readOnly key={`${entity.name}${column.name}description`}
                        value={lookupAPI.lookupResult?.description} rightSection={lookupAPI.status.loading && <Loader size="xs" variant="bars" />}
                        ref={HTMLDescriptionRef} sx={{ flexGrow: 1 }} />
                </Group>
            </Group>
            {lookupAPI.lookupResult?.error && <Group><Text size="xs" color="red">{lookupAPI.lookupResult.errorDescription}</Text></Group>}
        </Stack>
    );
}
