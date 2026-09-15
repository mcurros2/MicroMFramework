const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

function loadUseTextTransform() {
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src/UI/Core/useTextTransform.ts'), 'utf8');
    const output = ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
    }).outputText;

    vm.runInNewContext(output, {
        exports,
        require: moduleName => {
            if (moduleName === 'react') return { useCallback: callback => callback };
            return {};
        },
    });

    return exports.useTextTransform;
}

function createTransform(options = {}) {
    const updates = [];
    const useTextTransform = loadUseTextTransform();
    const transform = useTextTransform({
        entityForm: { form: { setFieldValue: (column, value) => updates.push({ column, value }) } },
        column: { name: 'code' },
        ...options,
    });

    return { transform, updates };
}

test('returns the same normalized value that it writes to the form', () => {
    const { transform, updates } = createTransform({ autoTrim: true, transform: 'uppercase' });

    const result = transform('  abc  ');

    assert.equal(result, 'ABC');
    assert.deepEqual(updates, [{ column: 'code', value: 'ABC' }]);
});

test('returns the original value without writing when no transformation is configured', () => {
    const { transform, updates } = createTransform();

    const result = transform('abc');

    assert.equal(result, 'abc');
    assert.deepEqual(updates, []);
});
