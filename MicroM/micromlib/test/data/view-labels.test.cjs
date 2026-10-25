const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

const exportsObject = {};
const source = fs.readFileSync(path.join(__dirname, '../../src/UI/Core/dataViewLabels.ts'), 'utf8');
const output = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
}).outputText;
vm.runInNewContext(output, { exports: exportsObject });
const { getSelectedRowsLabel, getViewErrorMessage } = exportsObject;

test('selection labels distinguish zero, one and many without changing the row count', () => {
    const labels = { selectedRowLabel: 'fila seleccionada.', selectedRowsLabel: 'filas seleccionadas.' };
    assert.equal(getSelectedRowsLabel(0, labels), '');
    assert.equal(getSelectedRowsLabel(1, labels), 'fila seleccionada.');
    assert.equal(getSelectedRowsLabel(2, labels), 'filas seleccionadas.');
    assert.equal(getSelectedRowsLabel(100, labels), 'filas seleccionadas.');
});

test('existing consumers retain the plural label when a singular label is not supplied', () => {
    assert.equal(getSelectedRowsLabel(1, { selectedRowsLabel: 'rows selected.' }), 'rows selected.');
    assert.equal(getSelectedRowsLabel(2, { selectedRowsLabel: 'rows selected.' }), 'rows selected.');
    assert.equal(getSelectedRowsLabel(1), '');
});

test('network failures can be translated without mutating diagnostic details', () => {
    const error = Object.freeze({ message: 'Failed to fetch', url: '/views', errorBody: 'diagnostic' });
    assert.equal(getViewErrorMessage(error, 'No se pudo obtener la información'), 'No se pudo obtener la información');
    assert.equal(getViewErrorMessage({ ...error, status: 0 }, 'Sin conexión'), 'Sin conexión');
    assert.equal(getViewErrorMessage(error), 'Failed to fetch');
    assert.equal(error.message, 'Failed to fetch');
    assert.equal(error.url, '/views');
    assert.equal(error.errorBody, 'diagnostic');
});

test('HTTP, permission and business errors retain their status and details', () => {
    for (const error of [
        { status: 403, message: 'Forbidden', statusMessage: 'Access denied' },
        { status: 500, message: 'Failed to fetch', statusMessage: 'Server error' },
        { status: 11, statusMessage: 'No hay registros' },
        { message: 'Business rule failed' },
    ]) {
        const expected = [error.status, error.message, error.statusMessage].filter(v => v !== undefined).join(' ');
        assert.equal(getViewErrorMessage(error, 'No se pudo obtener la información'), expected);
    }
});
