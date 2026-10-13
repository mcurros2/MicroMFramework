import { FocusTrap, MantineProvider, Radio } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useEffect, useMemo, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import type { SQLType, Value, ValuesObject } from '../../src/client';
import type { Entity, EntityDefinition } from '../../src/Entity';
import { EntityColumn } from '../../src/Entity/EntityColumn';
import { EntityColumnFlags } from '../../src/Entity/EntityColumn.types';
import { AddressAutocomplete } from '../../src/UI/AddressInput/AddressAutocomplete';
import { AddressLookup } from '../../src/UI/AddressInput/AddressLookup';
import { AutoFormFields } from '../../src/UI/AutoForm/AutoFormFields';
import { BytesRangeField } from '../../src/UI/Core/BytesRangeField';
import { CheckboxField } from '../../src/UI/Core/CheckboxField';
import { CUITField } from '../../src/UI/Core/CUITField';
import { DateInputField } from '../../src/UI/Core/DateInputField';
import { EmailField } from '../../src/UI/Core/EmailField';
import type { FieldAutoFocus, FieldWidthProps } from '../../src/UI/Core/fieldUIHelpers';
import { ModalsManager } from '../../src/UI/Core/ModalsManager';
import { MoneyRangeField } from '../../src/UI/Core/MoneyRangeField';
import { NumberField } from '../../src/UI/Core/NumberField';
import { PasswordField } from '../../src/UI/Core/PasswordField';
import { PhoneField } from '../../src/UI/Core/PhoneField';
import { PinField } from '../../src/UI/Core/PinField';
import { RadioGroupField } from '../../src/UI/Core/RadioGroupField';
import { SwitchField } from '../../src/UI/Core/SwitchField';
import { TextAreaField } from '../../src/UI/Core/TextAreaField';
import { TextField } from '../../src/UI/Core/TextField';
import { TimeField } from '../../src/UI/Core/TimeField';
import type { FormMode } from '../../src/UI/Core/types';
import { UrlField } from '../../src/UI/Core/UrlField';
import { WeekPickerField } from '../../src/UI/Core/WeekPickerField';
import type { UseEntityFormReturnType } from '../../src/UI/Form';
import { EntityForm } from '../../src/UI/Form/EntityForm';
import { Lookup } from '../../src/UI/Lookup/Lookup';
import { LookupMultiSelect } from '../../src/UI/LookupMultiSelect/LookupMultiSelect';
import { LookupSelect } from '../../src/UI/LookupSelect/LookupSelect';

const noop = () => undefined;

function makeColumn(name: string, type: SQLType, value: Value, flags: number) {
    const column = new EntityColumn({ name, type, value, flags, length: 20, prompt: 'Field' });
    column.value = value;
    return column;
}

function Fixture({ kind }: { kind: string }) {
    const params = new URLSearchParams(location.search);
    const mode = (params.get('mode') ?? 'add') as FormMode;
    const [loading, setLoading] = useState<boolean | undefined>(params.get('loading') === 'true' ? true : params.get('loading') === 'undefined' ? undefined : false);
    const readOnly = params.has('readonly') ? params.get('readonly') === 'true' : undefined;
    const legacyReadOnly = params.has('legacy') ? params.get('legacy') === 'true' : undefined;
    const autoFocus = params.get('focus') === 'true' ? true : params.get('focus') === 'false' ? false : params.get('focus') as FieldAutoFocus | undefined;
    const disabled = params.get('disabled') === 'true';
    const numeric = ['NumberField', 'MoneyRangeField', 'BytesRangeField', 'AutoForm'].includes(kind);
    const boolean = ['CheckboxField', 'SwitchField'].includes(kind);
    const date = ['DateInputField', 'WeekPickerField'].includes(kind);
    const type: SQLType = numeric ? 'int' : boolean ? 'bit' : date ? 'date' : 'varchar';
    const initialValue = numeric ? 123 : boolean ? false : date ? new Date(2026, 9, 12) : kind === 'TimeField' ? '12:30' : kind === 'RadioGroupField' ? 'b' : kind === 'PinField' ? '1234' : kind === 'LookupMultiSelect' ? ['a'] : params.get('value') ?? 'abc';
    const [columns] = useState(() => ({
        value: makeColumn('value', type, initialValue, EntityColumnFlags.nullable | (params.get('flag') === 'pk' ? EntityColumnFlags.pk : params.get('flag') === 'autoNum' ? EntityColumnFlags.autoNum : 0)),
        prefix: makeColumn('prefix', 'varchar', 'p', EntityColumnFlags.nullable),
        second: makeColumn('second', 'int', 456, EntityColumnFlags.nullable),
        third: makeColumn('third', 'bit', false, EntityColumnFlags.nullable),
    }));
    const form = useForm<ValuesObject>({ initialValues: Object.fromEntries(Object.entries(columns).map(([name, column]) => [name, column.value])) });
    columns.prefix.excludeInAutoForm = kind === 'AutoForm';
    const [entity] = useState(() => {
        const lookupColumns = {
            key: makeColumn('key', 'varchar', '', EntityColumnFlags.pk),
            prefix: makeColumn('prefix', 'varchar', '', EntityColumnFlags.pk),
            value: makeColumn('value', 'varchar', '', EntityColumnFlags.pk),
        };
        const view = { name: 'list', keyMappings: { key: 0 }, compoundKeyGroups: { tuple: { viewIndex: 0, keyMappings: { prefix: 0, value: 1 } } } };
        const API = {
            client: { TIMEZONE_OFFSET: 0 },
            executeView: async () => [{ records: [['a', 'Alpha'], ['b', 'Beta']], typeInfo: ['varchar', 'varchar'] }],
            lookupData: async () => 'Description',
        };
        const lookupEntity = { name: 'FixtureLookup', API, def: { name: 'FixtureLookup', columns: lookupColumns, views: { list: view }, standardView: () => 'list' } };
        return {
            name: 'Fixture', API,
            def: { name: 'Fixture', columns, lookups: { list: { name: 'list', bindingColumnKey: 'key', compoundKeyGroupName: 'tuple', entityConstructor: () => lookupEntity } } },
        } as unknown as Entity<EntityDefinition>;
    });
    const showDescriptionState = useState(false);
    const notifyValidationErrorState = useState(false);
    const status = useMemo(() => ({ loading }), [loading]);
    const entityForm = {
        entity, form, formMode: mode, status, showDescriptionState,
        configureField: noop, removeValidation: noop,
        notifyValidationErrorState, isFormValid: () => true, asyncErrors: {}, activeFormActions: {},
        navigationGuard: { mode: 'disabled', hasUnsavedChanges: () => false, onSave: async () => true, onLeave: async () => true },
        handleSubmit: noop, handleCancel: noop,
    } as unknown as UseEntityFormReturnType;
    const widthValue = (name: string) => params.get(name) === '0' ? 0 : params.get(name) ?? undefined;
    const widths: FieldWidthProps = {
        maw: widthValue('maw'), miw: widthValue('miw'),
        maxWidth: params.get('max') as FieldWidthProps['maxWidth'] ?? undefined,
        minWidth: params.get('min') as FieldWidthProps['minWidth'] ?? undefined,
    };
    if (params.get('responsive')) widths.maw = { base: '21rem', md: '42rem' };
    const common = { column: columns.value, entityForm, readOnly, autoFocus, disabled, ...widths };
    const timeRef = useRef<{ showPicker: () => void }>(null);
    const [blurCalls, setBlurCalls] = useState(0);
    useEffect(() => {
        (window as unknown as { fieldFixture: { finishLoading: () => void, showPicker: () => void, setValidationError: () => void } }).fieldFixture = {
            finishLoading: () => setLoading(false), showPicker: () => timeRef.current?.showPicker(),
            setValidationError: () => form.setFieldError('value', 'Fixture validation'),
        };
    }, []);
    let field;
    switch (kind) {
        case 'TextField': field = <TextField {...common} onBlur={() => setBlurCalls(count => count + 1)} />; break;
        case 'TextAreaField': field = <TextAreaField {...common} autoTrim />; break;
        case 'EmailField': field = <EmailField {...common} />; break;
        case 'UrlField': field = <UrlField {...common} />; break;
        case 'CUITField': field = <CUITField {...common} />; break;
        case 'PhoneField': field = <PhoneField {...common} />; break;
        case 'NumberField': field = <NumberField {...common} />; break;
        case 'MoneyRangeField': field = <MoneyRangeField {...common} />; break;
        case 'BytesRangeField': field = <BytesRangeField {...common} />; break;
        case 'PasswordField': field = <PasswordField {...common} column={columns.value as EntityColumn<string>} />; break;
        case 'DateInputField': field = <DateInputField {...common} clearable />; break;
        case 'TimeField': field = <TimeField {...common} ref={timeRef} />; break;
        case 'PinField': field = <PinField {...common} />; break;
        case 'CheckboxField': field = <CheckboxField {...common} />; break;
        case 'SwitchField': field = <SwitchField {...common} />; break;
        case 'RadioGroupField': field = <RadioGroupField {...common}><Radio value="a" label="Alpha" /><Radio value="b" label="Beta" /></RadioGroupField>; break;
        case 'WeekPickerField': field = <WeekPickerField {...common} readonly={legacyReadOnly} weekStartDateColumn={columns.value as EntityColumn<Date | null>} accordionExpanded />; break;
        case 'LookupSelect': field = <LookupSelect entityForm={entityForm} column={columns.value} entity={entity} lookupDefName="list" autoFocus={autoFocus} minWidth={widths.minWidth} maxWidth={widths.maxWidth} enableEdit selectProps={{ readOnly, disabled, maw: widths.maw, miw: widths.miw }} />; break;
        case 'LookupMultiSelect': field = <LookupMultiSelect {...common} entity={entity} lookupDefName="list" creatable />; break;
        case 'Lookup': field = <Lookup {...common} readonly={legacyReadOnly} entity={entity} lookupDefName="list" />; break;
        case 'CompoundLookup': field = <Lookup entityForm={entityForm} entity={entity} lookupDefName="list" bindingColumns={[columns.prefix, columns.value]} readOnly={readOnly} readonly={legacyReadOnly} disabled={disabled} autoFocus={autoFocus} {...widths} />; break;
        case 'AddressAutocomplete': field = <AddressAutocomplete {...common} countries={['ar']} />; break;
        case 'AddressLookup': field = <AddressLookup {...common} countries={['ar']} />; break;
        case 'AutoForm': field = <AutoFormFields entity={entity} entityForm={entityForm} />; break;
        default: throw new Error(`Unknown field: ${kind}`);
    }
    return <>
        <div data-testid="field" style={{ padding: 20 }}>
            {params.get('entityForm') ? <EntityForm formAPI={entityForm} formHeight="auto" showLoadingProgress={false} showOK={false} showCancel={false}>{field}</EntityForm> :
                <FocusTrap active={loading === false}><div><fieldset disabled={loading} style={{ border: 0, padding: 0, margin: 0 }}>{field}</fieldset></div></FocusTrap>}
        </div>
        <button id="outside">Outside</button>
        <output id="value">{JSON.stringify(form.values.value)}</output>
        <output id="blur">{blurCalls}</output>
    </>;
}

const kind = new URLSearchParams(location.search).get('kind') ?? 'TextField';
createRoot(document.getElementById('root')!).render(
    <MantineProvider withGlobalStyles withNormalizeCSS>
        <ModalsManager modalProps={{}} animationDuration={0}><Fixture kind={kind} /></ModalsManager>
    </MantineProvider>
);
