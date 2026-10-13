import { ActionIcon, getSize, Group, Loader, rem, Stack, Text, TextInput, useComponentDefaultProps, useMantineTheme } from "@mantine/core";
import { isNotEmpty } from "@mantine/form";
import { useEffect, useMemo, useRef } from "react";
import { EntityColumnFlags } from "../../Entity";
import { resolveFieldAutoFocus, resolveFieldReadOnly, resolveFieldWidths } from "../Core/fieldUIHelpers";
import { CompoundLookupProps } from "./Lookup";
import { CompoundLookupDefaultProps } from "./Lookup.shared";
import { useCompoundLookup } from "./useCompoundLookup";


export function CompoundLookup(props: CompoundLookupProps) {
    const {
        entityForm, entity, lookupDefName, autoFocus, label, parentKeys, bindingColumns, required, readonly, disabled,
        idMaxWidth, icon, iconVariant, requiredLabel, description, size, onLookupPerformed,
        enableAdd, enableEdit, enableDelete, enableView, transform, autoTrim, editLastLevelOnly, prefixMaxWidth, readOnly, maw, miw, maxWidth, minWidth
    } = useComponentDefaultProps('CompoundLookup', CompoundLookupDefaultProps, props);

    if (bindingColumns.length < 2) throw new Error('Compound Lookup requires at least two bindingColumns.');

    const theme = useMantineTheme();

    const significantColumn = bindingColumns[bindingColumns.length - 1];
    const bindingColumnNames = useMemo(() => bindingColumns.map(column => column.name), [bindingColumns]);

    const inputRef = useRef<HTMLInputElement>(null);

    useEffect(() => {
        const significantColumnIsRequired = required ?? !significantColumn.hasFlag(EntityColumnFlags.nullable);
        bindingColumns.forEach((column, index) => {
            const validation = index === bindingColumns.length - 1 && significantColumnIsRequired
                ? isNotEmpty(requiredLabel)
                : undefined;
            entityForm.configureField(column, validation);
        });
    }, [bindingColumns, entityForm, required, requiredLabel, significantColumn]);

    const lookupAPI = useCompoundLookup({
        entityForm, entity, lookupDefName, bindingColumns: bindingColumnNames, parentKeys, required,
        inputRef, enableAdd, enableEdit, enableDelete, enableView, transform, autoTrim, editLastLevelOnly,
    });

    useEffect(() => {
        if (!lookupAPI.lookupResult) return;
        bindingColumns.forEach((column, index) => column.valueDescription = index === bindingColumns.length - 1 ? lookupAPI.lookupResult?.description : undefined);
    }, [bindingColumns, lookupAPI.lookupResult]);

    useEffect(() => { if (onLookupPerformed && lookupAPI.lookupResult) onLookupPerformed(lookupAPI.lookupResult); }, [lookupAPI.lookupResult, onLookupPerformed]);

    const controlSize = getSize({ size: size!, sizes: theme.fontSizes });
    const descriptionSize = getSize({ size: size!, sizes: theme.fontSizes });
    const labelColor = theme.colorScheme === 'dark' ? theme.colors.dark[0] : theme.colors.gray[9];
    const descriptionColor = theme.colorScheme === 'dark' ? theme.colors.dark[2] : theme.colors.gray[6];
    const { formMode, status } = entityForm;
    const resolvedAutoFocus = resolveFieldAutoFocus(autoFocus, formMode, status.loading);
    const widths = resolveFieldWidths({ maw, miw, maxWidth, minWidth });

    const readonlyCondition = resolveFieldReadOnly(significantColumn, formMode, readOnly ?? readonly);

    return (
        <Stack {...widths} style={{ gap: "0.1rem", flexGrow: "1" }}>
            <Group style={{ gap: "0.2rem" }}>
                <Text size={controlSize} weight="500" color={labelColor}>{label ?? significantColumn.prompt}</Text>
                {(required ?? (!readonly && entityForm.formMode !== 'view' && !significantColumn.hasFlag(EntityColumnFlags.nullable))) && <Text size={controlSize} weight="500" color={theme.colors.red[5]}>*</Text>}
            </Group>
            {(description ?? significantColumn.description) && <Text style={{ fontSize: `calc(${descriptionSize} - ${rem(2)})`, lineHeight: 1.2 }} color={descriptionColor}>{description ?? significantColumn.description}</Text>}
            <Group style={(description ?? significantColumn.description) ? { marginTop: `calc(${theme.spacing.xs} / 2)` } : undefined} align="flex-start">
                <Group style={{ flexGrow: 1 }} maw={idMaxWidth}>
                    {editLastLevelOnly &&
                        <TextInput readOnly value={lookupAPI.keyPrefix} maw={prefixMaxWidth} />
                    }
                    <Group style={{ flex: "1 1" }}>
                        <TextInput ref={inputRef} size={size} sx={{ flexGrow: 1 }}
                            readOnly={readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading}
                            autoFocus={resolvedAutoFocus}
                            data-autofocus={resolvedAutoFocus ? true : undefined}
                            disabled={disabled} key={`${entity.name}${significantColumn.name}Compound`}
                            rightSection={<ActionIcon color={theme.primaryColor}
                                disabled={disabled || readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading}
                                onClick={async () => {
                                    if (disabled || readonlyCondition || entityForm.formMode === 'view' || lookupAPI.status.loading || entityForm.status.loading) return;
                                    await lookupAPI.onBlur(significantColumn.name, true);
                                }}
                                variant={iconVariant} size="md">{icon}</ActionIcon>}
                            {...lookupAPI.lookupInputProps}
                            onBlur={async event => { await lookupAPI.onBlur(significantColumn.name, false, event); }}
                        />
                    </Group>
                </Group>
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
