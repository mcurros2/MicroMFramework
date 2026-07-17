import { ActionIcon, getSize, Group, Loader, rem, Stack, Text, TextInput, useMantineTheme } from "@mantine/core";
import { isNotEmpty } from "@mantine/form";
import { IconSearch } from "@tabler/icons-react";
import { useEffect, useMemo, useRef } from "react";
import { Value } from "../../client";
import { EntityColumn, EntityColumnFlags } from "../../Entity";
import { LookupCommonProps } from "./Lookup.shared";
import { useCompoundLookup } from "./useCompoundLookup";

type CompoundLookupProps = LookupCommonProps & { column?: never, bindingColumns: EntityColumn<Value>[] };

export function CompoundLookup(props: CompoundLookupProps) {
    const {
        entityForm, entity, lookupDefName, autoFocus, label, parentKeys, bindingColumns, required, readonly, disabled,
        idMaxWidth = "15rem", icon = <IconSearch size="1rem" stroke="1.5" />, iconVariant = "light",
        requiredLabel = "A value is required", description, size = "sm", onLookupPerformed,
        enableAdd = false, enableEdit = false, enableDelete = false, enableView = true, transform, autoTrim = true,
    } = props;
    if (bindingColumns.length < 2) throw new Error('Compound Lookup requires at least two bindingColumns.');

    const significantColumn = bindingColumns[bindingColumns.length - 1];
    const bindingColumnKey = bindingColumns.map(column => column.name).join('\u0000');
    const bindingColumnNames = useMemo(() => bindingColumns.map(column => column.name), [bindingColumnKey]);
    const theme = useMantineTheme();
    const inputRef = useRef<HTMLInputElement>(null);
    const lookupAPI = useCompoundLookup({
        entityForm, entity, lookupDefName, bindingColumns: bindingColumnNames, parentKeys, required,
        inputRef, enableAdd, enableEdit, enableDelete, enableView, transform, autoTrim,
    });

    useEffect(() => {
        if (required ?? !significantColumn.hasFlag(EntityColumnFlags.nullable)) entityForm.configureField(significantColumn, isNotEmpty(requiredLabel));
        else entityForm.removeValidation(significantColumn);
    }, [entityForm, required, requiredLabel, significantColumn]);
    useEffect(() => {
        bindingColumns.forEach((column, index) => column.valueDescription = index === bindingColumns.length - 1 ? lookupAPI.lookupResult?.description : undefined);
    }, [bindingColumns, lookupAPI.lookupResult?.description]);
    useEffect(() => { if (onLookupPerformed && lookupAPI.lookupResult) onLookupPerformed(lookupAPI.lookupResult); }, [lookupAPI.lookupResult, onLookupPerformed]);

    const controlSize = getSize({ size, sizes: theme.fontSizes });
    const descriptionSize = getSize({ size, sizes: theme.fontSizes });
    const labelColor = theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[9];
    const descriptionColor = theme.colorScheme === 'dark' ? theme.colors.dark[2] : theme.colors.gray[6];
    const { formMode, status } = entityForm;
    const addAutofocus = formMode === 'add' ? true : undefined;
    const editAutofocus = status.loading === false && formMode !== 'add' ? true : undefined;
    const readonlyCondition = readonly === undefined
        ? significantColumn.hasFlag(EntityColumnFlags.autoNum) || (entityForm.formMode !== 'add' && significantColumn.hasFlag(EntityColumnFlags.pk))
        : readonly;

    return (
        <Stack style={{ gap: "0.1rem", flexGrow: "1" }}>
            <Group style={{ gap: "0.2rem" }}>
                <Text size={controlSize} weight="500" color={labelColor}>{label ?? significantColumn.prompt}</Text>
                {(required ?? (!readonly && entityForm.formMode !== 'view' && !significantColumn.hasFlag(EntityColumnFlags.nullable))) && <Text size={controlSize} weight="500" color={theme.colors.red[5]}>*</Text>}
            </Group>
            {(description ?? significantColumn.description) && <Text style={{ fontSize: `calc(${descriptionSize} - ${rem(2)})`, lineHeight: 1.2 }} color={descriptionColor}>{description ?? significantColumn.description}</Text>}
            <Group style={{ marginTop: `calc(${theme.spacing.xs} / 2)` }} align="flex-start">
                <TextInput ref={inputRef} size={size} maw={idMaxWidth}
                    readOnly={readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading}
                    autoFocus={autoFocus === 'autoFocusOnAdd' ? addAutofocus : autoFocus === 'autoFocusOnEdit' ? editAutofocus : autoFocus}
                    data-autofocus={autoFocus === 'autoFocusOnAdd' ? addAutofocus : autoFocus === 'autoFocusOnEdit' ? editAutofocus : autoFocus}
                    disabled={disabled} key={`${entity.name}${significantColumn.name}Compound`}
                    rightSection={<ActionIcon color={theme.primaryColor}
                        disabled={readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading}
                        onClick={async () => {
                            if (readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading) return;
                            await lookupAPI.onBlur(significantColumn.name, true);
                        }}
                        variant={iconVariant} size="md">{icon}</ActionIcon>}
                    {...lookupAPI.lookupInputProps}
                    onBlur={async event => { await lookupAPI.onBlur(significantColumn.name, false, event); }}
                />
                <Group style={{ flexGrow: 1 }}>
                    <TextInput size={size} readOnly key={`${entity.name}${significantColumn.name}CompoundDescription`}
                        value={lookupAPI.lookupResult?.description ?? ''} rightSection={lookupAPI.status.loading && <Loader size="xs" variant="bars" />}
                        sx={{ flexGrow: 1 }} />
                </Group>
            </Group>
            {lookupAPI.lookupResult?.error && lookupAPI.lookupResult.errorDescription && <Group><Text size="xs" color="red">{lookupAPI.lookupResult.errorDescription}</Text></Group>}
        </Stack>
    );
}
