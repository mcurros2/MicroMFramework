import { Stack } from "@mantine/core";
import { Lookup, LookupMultiSelect, LookupSelect, UseEntityFormReturnType } from "@mcurros2/microm";
import { useMemo } from "react";
import { FieldsDemo } from "./entities";

export interface LookupsSectionProps {
    entity: FieldsDemo,
    entityForm: UseEntityFormReturnType,
    show?: ("lookup" | "select" | "multiselect" | "compound" | "compoundLastLevel")[],
}

export function LookupsSection({ entity, entityForm, show = ["lookup", "select", "multiselect", "compound"] }: LookupsSectionProps) {
    const cols = entity.def.columns;
    const binding = useMemo(() => [cols.c_country_id, cols.c_province_id], [cols]);
    const country = entityForm.form.values.c_country_id as string | null;
    const countryKeys = useMemo(() => ({ c_country_id: country }), [country]);

    return (
        <Stack spacing="xs">
            {show.includes("lookup") &&
                <Lookup entityForm={entityForm} entity={entity} column={cols.c_category_id} lookupDefName={entity.def.lookups.Categories.name} required={false} />}
            {show.includes("select") &&
                <LookupSelect entityForm={entityForm} formStatus={entityForm.status} entity={entity} column={cols.c_category2_id}
                    lookupDefName={entity.def.lookups.Categories.name} enableEdit />}
            {show.includes("multiselect") &&
                <LookupMultiSelect entityForm={entityForm} formStatus={entityForm.status} entity={entity} column={cols.vc_tags}
                    lookupDefName={entity.def.lookups.Tags.name} />}
            {show.includes("compound") &&
                <Lookup entityForm={entityForm} entity={entity} bindingColumns={binding} lookupDefName={entity.def.lookups.Provinces.name}
                    required={false} label="Country - Province" description="CompoundLookup: type AR-CB or pick from the grid" />}
            {show.includes("compoundLastLevel") &&
                <>
                    <Lookup entityForm={entityForm} entity={entity} column={cols.c_country_id} lookupDefName={entity.def.lookups.Countries.name} required={false} />
                    <Lookup entityForm={entityForm} entity={entity} bindingColumns={binding} lookupDefName={entity.def.lookups.Provinces.name}
                        parentKeys={countryKeys} editLastLevelOnly required={false} label="Province" description="editLastLevelOnly: country comes from the field above" />
                </>}
        </Stack>
    );
}
