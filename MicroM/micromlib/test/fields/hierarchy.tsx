import { MantineProvider } from '@mantine/core';
import { useForm } from '@mantine/form';
import { StrictMode, useCallback, useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import type { ValuesObject } from '../../src/client';
import type { Entity, EntityDefinition } from '../../src/Entity';
import { EntityColumn } from '../../src/Entity/EntityColumn';
import { EntityColumnFlags } from '../../src/Entity/EntityColumn.types';
import { ModalsManager } from '../../src/UI/Core/ModalsManager';
import { useHierarchyKeys } from '../../src/UI/Core/useHierarchyKeys';
import type { UseEntityFormReturnType } from '../../src/UI/Form';
import { LookupSelect } from '../../src/UI/LookupSelect/LookupSelect';

const names = ['campus', 'building', 'room'];
const mappedNames = ['lookupCampus', 'lookupBuilding', 'lookupRoom'];
const defaults = new URLSearchParams(location.search).has('defaults');
const mapped = new URLSearchParams(location.search).has('mapped');
const clears: string[] = [];
const requests: { name: string, keys?: ValuesObject }[] = [];
const commits: { values: ValuesObject, keys: ValuesObject[], sameKeys: boolean }[] = [];
const noop = () => undefined;

function Fixture() {
    const form = useForm<ValuesObject>({ initialValues: defaults ? { campus: null } : { campus: '', building: '', room: '' } });
    const [status, setStatus] = useState<UseEntityFormReturnType['status']>({});
    const [formMode, setFormMode] = useState<UseEntityFormReturnType['formMode']>('add');
    const [, setTick] = useState(0);
    const [entity] = useState(() => {
        const column = (name: string, value = '') => new EntityColumn({ name, type: 'varchar', length: 20, value, prompt: name, flags: EntityColumnFlags.nullable });
        const columns = Object.fromEntries(names.map(name => [name, column(name, defaults && name === 'building' ? 'default-building' : '')]));
        const client = { TIMEZONE_OFFSET: 0 };
        const lookups = Object.fromEntries(names.map(name => [name, {
            name, bindingColumnKey: 'key',
            entityConstructor: (_client: unknown, parentKeys?: ValuesObject) => ({
                name: `${name}Lookup`,
                def: { name: `${name}Lookup`, columns: { key: column('key') }, views: { list: { name: 'list', keyMappings: { key: 0 } } }, standardView: () => 'list' },
                API: { client, executeView: async (_view: unknown, keys?: ValuesObject) => {
                    requests.push({ name, keys });
                    const campus = keys?.campus ?? keys?.lookupCampus;
                    const records = name === 'campus' ? [['c1', 'Campus One'], ['c2', 'Campus Two']]
                        : name === 'building' ? (campus === 'c1' ? [['b1', 'Building One']] : campus === 'c2' ? [['b2', 'Building Two']] : [])
                        : [['r1', 'Room One'], ['r2', 'Room Two']];
                    return [{ records, typeInfo: ['varchar', 'varchar'] }];
                } },
                parentKeys,
            }),
        }]));
        return { name: 'HierarchyFixture', API: { client }, def: { columns, lookups } } as unknown as Entity<EntityDefinition>;
    });
    const { setFieldValue } = form;
    // Only count writes from the hook; control onChange still uses Mantine's binding.
    const clearFieldValue = useCallback<typeof setFieldValue>((name, value) => {
        clears.push(String(name));
        setFieldValue(name, value);
    }, [setFieldValue]);
    const showDescriptionState = useState(false);
    const notifyValidationErrorState = useState(false);
    const formAPI = {
        form, entity, status, formMode, showDescriptionState, notifyValidationErrorState,
        configureField: noop, removeValidation: noop,
    } as unknown as UseEntityFormReturnType;
    const keys = useHierarchyKeys({
        formAPI: { ...formAPI, form: { ...form, setFieldValue: clearFieldValue } },
        hierarchy: [...names],
        mappedHierarchy: mapped ? [...mappedNames] : undefined,
    });
    const previousKeys = useRef<ValuesObject[]>();
    useEffect(() => {
        commits.push({ values: form.values, keys, sameKeys: previousKeys.current === keys });
        previousKeys.current = keys;
        // Fail a broken fixture promptly instead of leaving an infinite loop in the browser.
        if (commits.length > 150) throw new Error('Hierarchy renders did not settle');
        Object.assign(window, { hierarchyFixture: {
            snapshot: () => ({ values: form.values, keys, clears: [...clears], requests: [...requests], commits: [...commits] }),
            setValues: form.setValues,
            rerender: () => setTick(value => value + 1),
            beginLoad: () => { setFormMode('edit'); setStatus({ operationType: 'get', loading: true }); },
            finishLoad: (values: ValuesObject) => { form.setValues(values); setStatus({ operationType: 'get', loading: false }); },
        } });
    });
    return <>
        {names.map((name, index) => <LookupSelect key={name} entity={entity} entityForm={formAPI}
            column={entity.def.columns[name]} lookupDefName={name} parentKeys={keys[index]}
            selectProps={{ label: name, readOnly: false, clearable: true, searchable: true }} />)}
        <output id="hierarchy-values">{JSON.stringify(form.values)}</output>
    </>;
}

createRoot(document.getElementById('root')!).render(
    <StrictMode><MantineProvider withGlobalStyles withNormalizeCSS>
        <ModalsManager modalProps={{}} animationDuration={0}><Fixture /></ModalsManager>
    </MantineProvider></StrictMode>
);
