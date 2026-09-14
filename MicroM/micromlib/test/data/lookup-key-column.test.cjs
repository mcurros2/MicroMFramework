const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

function loadResolver() {
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src/UI/Lookup/resolveLookupKeyColumn.ts'), 'utf8');

    vm.runInNewContext(ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
    }).outputText, { exports });

    return exports.resolveLookupKeyColumn;
}

function createLookupEntity(views, columns) {
    return {
        def: {
            name: 'Dealers',
            views,
            columns: Object.fromEntries(columns.map(columnName => [columnName, {}])),
        },
    };
}

test('infers a differently named form binding from the selected view key mapping', () => {
    const resolveLookupKeyColumn = loadResolver();
    const lookupDef = {
        name: 'Dealers',
        entityConstructor: () => undefined,
        viewMapping: { keyIndex: 0, descriptionIndex: 1 },
    };
    const lookupEntity = createLookupEntity({
        deal_brwStandard: { name: 'deal_brwStandard', keyMappings: { c_dealer_id: 0 } },
    }, ['c_dealer_id']);

    const result = resolveLookupKeyColumn(lookupDef, lookupEntity, 'deal_brwStandard', 'c_dealer_replacement_id');

    assert.deepEqual({ ...result }, { columnName: 'c_dealer_id' });
});

test('matches keyMappings by result index rather than object-key position', () => {
    const resolveLookupKeyColumn = loadResolver();
    const lookupDef = {
        name: 'Dealers',
        entityConstructor: () => undefined,
        viewMapping: { keyIndex: 4, descriptionIndex: 1 },
    };
    const lookupEntity = createLookupEntity({
        custom: { name: 'custom', keyMappings: { secondary_key: 0, primary_key: 4 } },
    }, ['secondary_key', 'primary_key']);

    const result = resolveLookupKeyColumn(lookupDef, lookupEntity, 'custom', 'replacement_id');

    assert.deepEqual({ ...result }, { columnName: 'primary_key' });
});

test('explicit bindingColumnKey overrides inference', () => {
    const resolveLookupKeyColumn = loadResolver();
    const lookupDef = {
        name: 'RepuestosAlternativos',
        entityConstructor: () => undefined,
        bindingColumnKey: 'explicit_key',
        viewMapping: { keyIndex: 0, descriptionIndex: 1 },
    };
    const lookupEntity = createLookupEntity({
        standard: { name: 'standard', keyMappings: { inferred_key: 0 } },
    }, ['explicit_key', 'inferred_key']);

    const result = resolveLookupKeyColumn(lookupDef, lookupEntity, 'standard', 'form_key');

    assert.deepEqual({ ...result }, { columnName: 'explicit_key' });
});

test('defaults to result index zero and preserves the legacy same-name fallback', () => {
    const resolveLookupKeyColumn = loadResolver();
    const lookupDef = { name: 'Legacy', entityConstructor: () => undefined };
    const inferredEntity = createLookupEntity({
        standard: { name: 'standard', keyMappings: { inferred_key: 0 } },
    }, ['inferred_key']);
    const legacyEntity = createLookupEntity({
        standard: { name: 'standard', keyMappings: {} },
    }, ['form_key']);

    assert.deepEqual(
        { ...resolveLookupKeyColumn(lookupDef, inferredEntity, 'standard', 'form_key') },
        { columnName: 'inferred_key' }
    );
    assert.deepEqual(
        { ...resolveLookupKeyColumn(lookupDef, legacyEntity, 'standard', 'form_key') },
        { columnName: 'form_key' }
    );
});

test('returns descriptive errors for ambiguous or missing mappings', () => {
    const resolveLookupKeyColumn = loadResolver();
    const lookupDef = {
        name: 'InvalidLookup',
        entityConstructor: () => undefined,
        viewMapping: { keyIndex: 0, descriptionIndex: 1 },
    };
    const ambiguousEntity = createLookupEntity({
        standard: { name: 'standard', keyMappings: { first: 0, second: 0 } },
    }, ['first', 'second']);
    const missingEntity = createLookupEntity({
        standard: { name: 'standard', keyMappings: { another_key: 2 } },
    }, ['another_key']);

    const ambiguous = resolveLookupKeyColumn(lookupDef, ambiguousEntity, 'standard', 'form_key');
    const missing = resolveLookupKeyColumn(lookupDef, missingEntity, 'standard', 'form_key');

    assert.match(ambiguous.error, /multiple key mappings/);
    assert.match(missing.error, /cannot infer its key column/);
    assert.equal(ambiguous.columnName, undefined);
    assert.equal(missing.columnName, undefined);
});
