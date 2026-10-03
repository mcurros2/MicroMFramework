import { Divider, Group, Radio, SimpleGrid, Stack, Text } from "@mantine/core";
import {
    BytesRangeField, CheckboxField, CUITField, DateInputField, EmailField, EntityForm, EntityFormProps, FormMode, MoneyRangeField, NumberField,
    PasswordField, PhoneField, PinField, RadioGroupField, SwitchField, TextAreaField, TextField, TimeField, UrlField, useEntityForm, WeekPickerField
} from "@mcurros2/microm";
import { FieldsDemo } from "./entities";
import { LookupsSection } from "./LookupsSection";

export interface FieldsDemoFormProps extends Omit<EntityFormProps, 'formAPI' | 'children'> {
    entity: FieldsDemo,
    initialFormMode: FormMode,
}

export function Section({ title }: { title: string }) {
    return <Divider mt="sm" label={<Text fw={600} size="sm">{title}</Text>} labelPosition="left" />;
}

export function FieldsDemoForm(props: FieldsDemoFormProps) {
    const { entity, initialFormMode, ...rest } = props;

    const entityForm = useEntityForm({
        entity,
        initialFormMode,
        getDataOnInit: initialFormMode !== 'add',
        validateInputOnBlur: true,
    });

    const cols = entity.def.columns;

    return (
        <EntityForm formAPI={entityForm} showHelpButton {...rest}>
            <Stack spacing="xs">
                <Section title="Text" />
                <SimpleGrid cols={2} breakpoints={[{ maxWidth: 'sm', cols: 1 }]}>
                    <TextField entityForm={entityForm} column={cols.c_demo_id} />
                    <TextField entityForm={entityForm} column={cols.vc_text} />
                    <EmailField entityForm={entityForm} column={cols.vc_email} />
                    <PhoneField entityForm={entityForm} column={cols.vc_phone} />
                    <UrlField entityForm={entityForm} column={cols.vc_url} />
                    <CUITField entityForm={entityForm} column={cols.vc_cuit} />
                    <PasswordField entityForm={entityForm} column={cols.vc_password} required={false} />
                </SimpleGrid>

                <Section title="Numbers, dates and times" />
                <SimpleGrid cols={2} breakpoints={[{ maxWidth: 'sm', cols: 1 }]}>
                    <NumberField entityForm={entityForm} column={cols.i_quantity} />
                    <NumberField entityForm={entityForm} column={cols.m_amount} precision={2} />
                    <MoneyRangeField entityForm={entityForm} column={cols.m_budget} currencySymbol="$" />
                    <BytesRangeField entityForm={entityForm} column={cols.bi_quota} />
                    <DateInputField entityForm={entityForm} column={cols.dt_date} />
                    <TimeField entityForm={entityForm} column={cols.t_time} />
                </SimpleGrid>
                <WeekPickerField entityForm={entityForm} weekStartDateColumn={cols.dt_week_start} weekEndDateColumn={cols.dt_week_end} label={cols.dt_week_start.prompt} />

                <Section title="Options" />
                <Group align="flex-start" spacing="xl">
                    <CheckboxField entityForm={entityForm} column={cols.bt_terms} />
                    <SwitchField entityForm={entityForm} column={cols.bt_notifications} />
                </Group>
                <RadioGroupField entityForm={entityForm} column={cols.c_priority}>
                    <Group mt="xs">
                        <Radio value="LOW" label="Low" />
                        <Radio value="MEDIUM" label="Medium" />
                        <Radio value="HIGH" label="High" />
                    </Group>
                </RadioGroupField>

                <Section title="Lookups" />
                <LookupsSection entity={entity} entityForm={entityForm} />

                <Section title="Other" />
                <PinField entityForm={entityForm} column={cols.vc_pin} length={6} required={false} />
                <TextAreaField entityForm={entityForm} column={cols.vc_notes} minRows={3} />
            </Stack>
        </EntityForm>
    );
}
