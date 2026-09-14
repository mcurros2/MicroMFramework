const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

function loadUseRetainedSearch(state, setState = () => undefined) {
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src/UI/DataGrid/useRetainedSearch.ts'), 'utf8');

    vm.runInNewContext(ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
    }).outputText, {
        exports,
        require: moduleName => moduleName === '@mantine/hooks'
            ? { useSessionStorage: options => [state ?? options.defaultValue, setState] }
            : { useCallback: callback => callback },
    });

    return exports.useRetainedSearch;
}

test('retained search restores terms and requests execution without owning refreshOnInit', () => {
    const useRetainedSearch = loadUseRetainedSearch({
        version: 1,
        executed: true,
        terms: ['%order%'],
    });

    const result = useRetainedSearch({
        retainSearch: 'orders',
        search: ['initial'],
    });

    assert.deepEqual([...result.search], ['%order%']);
    assert.equal('refreshOnInit' in result, false);
    assert.equal(result.executeRetainedSearch, true);
});

test('retained search persists executions and clearing text resets retained execution', () => {
    const states = [];
    const searches = [];
    const textChanges = [];
    const useRetainedSearch = loadUseRetainedSearch(undefined, state => states.push(state));
    const result = useRetainedSearch({
        retainSearch: 'orders',
        onSearch: search => searches.push(search),
        onSearchTextChange: search => textChanges.push(search),
    });

    result.onSearch(['new']);
    assert.deepEqual({ ...states[0], terms: [...states[0].terms] }, {
        version: 1,
        executed: true,
        terms: ['new'],
    });
    assert.deepEqual([...searches[0]], ['new']);

    result.onSearchTextChange(undefined);
    assert.deepEqual({ ...states[1], terms: [...states[1].terms] }, {
        version: 1,
        executed: false,
        terms: [],
    });
    assert.equal(textChanges[0], undefined);
});

test('without retention, search and callbacks pass through unchanged', () => {
    const onSearch = () => undefined;
    const onSearchTextChange = () => undefined;
    const useRetainedSearch = loadUseRetainedSearch();
    const result = useRetainedSearch({
        search: ['initial'],
        onSearch,
        onSearchTextChange,
    });

    assert.deepEqual([...result.search], ['initial']);
    assert.equal(result.executeRetainedSearch, false);
    assert.equal(result.onSearch, onSearch);
    assert.equal(result.onSearchTextChange, onSearchTextChange);
});
