const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

function loadUseHierarchyKeys() {
    let hierarchyState;
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src/UI/Core/useHierarchyKeys.ts'), 'utf8');

    vm.runInNewContext(ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
    }).outputText, {
        exports,
        require: moduleName => {
            if (moduleName === 'react') {
                return {
                    useEffect: () => undefined,
                    useState: initializer => {
                        if (hierarchyState === undefined) {
                            hierarchyState = typeof initializer === 'function' ? initializer() : initializer;
                        }
                        return [hierarchyState, nextState => {
                            hierarchyState = typeof nextState === 'function' ? nextState(hierarchyState) : nextState;
                        }];
                    },
                };
            }
            if (moduleName === '../../Entity') {
                return {
                    areValuesObjectsEqual: (left, right) => JSON.stringify(left) === JSON.stringify(right),
                };
            }
            return {};
        },
    });

    return exports.useHierarchyKeys;
}

function createFormAPI(values, columnValues = {}) {
    return {
        form: {
            values,
            setFieldValue: () => undefined,
        },
        entity: {
            def: {
                columns: {
                    marca: { value: columnValues.marca },
                    dealer: { value: columnValues.dealer },
                },
            },
        },
        formMode: 'add',
        status: {},
    };
}

test('Add-mode field registration does not change initial hierarchy parent keys', () => {
    const useHierarchyKeys = loadUseHierarchyKeys();
    const formAPI = createFormAPI({});

    const initialKeys = useHierarchyKeys({ formAPI, hierarchy: ['marca', 'dealer'] });
    assert.deepEqual(initialKeys.map(keys => ({ ...keys })), [
        { marca: '' },
        { marca: '', dealer: '' },
    ]);

    formAPI.form.values = { marca: '', dealer: '' };
    const registeredKeys = useHierarchyKeys({ formAPI, hierarchy: ['marca', 'dealer'] });

    assert.equal(registeredKeys, initialKeys);
    assert.equal(registeredKeys[0], initialKeys[0]);
    assert.equal(registeredKeys[1], initialKeys[1]);

    formAPI.form.values = { marca: 'M', dealer: '' };
    const changedKeys = useHierarchyKeys({ formAPI, hierarchy: ['marca', 'dealer'] });
    assert.deepEqual(changedKeys.map(keys => ({ ...keys })), [
        { marca: 'M' },
        { marca: 'M', dealer: '' },
    ]);
});

test('missing hierarchy fields use column defaults while explicit null is preserved', () => {
    const useHierarchyKeys = loadUseHierarchyKeys();
    const formAPI = createFormAPI({ marca: null }, { marca: 'ignored', dealer: 'D' });

    const keys = useHierarchyKeys({ formAPI, hierarchy: ['marca', 'dealer'] });
    assert.deepEqual(keys.map(parentKeys => ({ ...parentKeys })), [
        { marca: null },
        { marca: null, dealer: 'D' },
    ]);
});
