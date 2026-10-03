import { Group, Stack } from "@mantine/core";
import {
    CheckboxField, DateInputField, EmailField, EntityForm, FormOptions, Lookup, LookupMultiSelect, LookupSelect, NumberField, PhoneField, TextAreaField,
    TextField, useEntityForm
} from "@mcurros2/microm";
import { useMemo } from "react";
import { Customers } from "./entities";

export function CustomersForm(props: FormOptions<Customers>) {
    const { entity, initialFormMode, getDataOnInit, onSaved, onCancel, ...rest } = props;

    const entityForm = useEntityForm({
        entity,
        initialFormMode,
        getDataOnInit: getDataOnInit ?? initialFormMode !== 'add',
        validateInputOnBlur: true,
        onSaved,
        onCancel,
    });

    const cols = entity.def.columns;
    const provinceBinding = useMemo(() => [cols.c_country_id, cols.c_province_id], [cols]);

    return (
        <EntityForm formAPI={entityForm} {...rest}>
            <Stack>
                {entityForm.formMode !== 'add' &&
                    <TextField entityForm={entityForm} column={cols.c_customer_id} maxWidth="xs" readOnly />
                }
                <TextField entityForm={entityForm} column={cols.vc_name} />
                <Group grow align="flex-start">
                    <EmailField entityForm={entityForm} column={cols.vc_email} />
                    <PhoneField entityForm={entityForm} column={cols.vc_phone} />
                </Group>
                <LookupSelect
                    entityForm={entityForm}
                    formStatus={entityForm.status}
                    entity={entity}
                    column={cols.c_category_id}
                    lookupDefName={entity.def.lookups.Categories.name}
                />
                <Lookup entityForm={entityForm} entity={entity} lookupDefName={entity.def.lookups.Provinces.name} bindingColumns={provinceBinding} required={false} label="Province" />
                <LookupMultiSelect entityForm={entityForm} formStatus={entityForm.status} entity={entity} column={cols.vc_tags} lookupDefName={entity.def.lookups.Tags.name} />
                <Group grow align="flex-start">
                    <DateInputField entityForm={entityForm} column={cols.dt_since} />
                    <NumberField entityForm={entityForm} column={cols.m_credit_limit} precision={2} />
                </Group>
                <CheckboxField entityForm={entityForm} column={cols.bt_active} />
                <TextAreaField entityForm={entityForm} column={cols.vc_notes} minRows={3} />
            </Stack>
        </EntityForm>
    );
}
