const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

function load(relativePath, dependencies = {}) {
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src', relativePath), 'utf8');
    const output = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText;
    vm.runInNewContext(output, { exports, require: name => dependencies[name] ?? {} });
    return exports;
}

const flags = load('Entity/EntityColumn.types.ts');
const types = load('UI/Core/types.ts');
const { resolveFieldReadOnly, resolveFieldAutoFocus, resolveFieldWidths } = load('UI/Core/fieldUI.ts', {
    '../../Entity': flags, './types': types,
});
const column = bits => ({ hasFlag: flag => (bits & flag) === flag });

test('readOnly honors column flags, explicit overrides, and mandatory view mode', () => {
    for (const mode of ['add', 'edit', 'view']) {
        for (const bits of [0, flags.EntityColumnFlags.pk, flags.EntityColumnFlags.autoNum]) {
            const expected = mode === 'view' || bits === flags.EntityColumnFlags.autoNum || (mode !== 'add' && bits === flags.EntityColumnFlags.pk);
            assert.equal(resolveFieldReadOnly(column(bits), mode), expected);
            assert.equal(resolveFieldReadOnly(column(bits), mode, false), mode === 'view');
            assert.equal(resolveFieldReadOnly(column(bits), mode, true), true);
        }
    }
});

test('widths preserve explicit string, zero, and responsive overrides', () => {
    for (const [token, width] of Object.entries(types.MicroMWidthSizes)) {
        const result = resolveFieldWidths({ minWidth: token, maxWidth: token });
        assert.equal(result.maw, token === 'auto' ? undefined : width);
        assert.equal(result.miw, token === 'auto' ? undefined : width);
    }
    const responsive = { base: '21rem', md: '42rem' };
    for (const value of ['42rem', 0, responsive]) {
        const result = resolveFieldWidths({ maw: value, miw: value, minWidth: 'sm', maxWidth: 'sm' });
        assert.equal(result.maw, value);
        assert.equal(result.miw, value);
    }
});

test('autofocus mode strings require the same loading state as TextField', () => {
    for (const mode of ['add', 'edit', 'view']) {
        for (const loading of [undefined, true, false]) {
            assert.equal(resolveFieldAutoFocus('autoFocusOnAdd', mode, loading), mode === 'add' ? true : undefined);
            assert.equal(resolveFieldAutoFocus('autoFocusOnEdit', mode, loading), mode !== 'add' && loading === false ? true : undefined);
            assert.equal(resolveFieldAutoFocus(false, mode, loading), false);
            assert.equal(resolveFieldAutoFocus(true, mode, loading), true);
            assert.equal(resolveFieldAutoFocus(undefined, mode, loading), undefined);
        }
    }
});

