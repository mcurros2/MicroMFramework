// Verify this working-tree change against HEAD before committing.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const ts = require('typescript');
const { ESLint } = require('eslint');

const git = process.env.GIT_EXECUTABLE || 'git';
const cwd = path.resolve(__dirname, '../..');
const changed = execFileSync(git, ['diff', '--name-only', '--', 'src/UI'], { cwd, encoding: 'utf8' }).trim().split(/\r?\n/).filter(Boolean);
const printer = ts.createPrinter({ removeComments: true });

function protectedExpressions(text) {
    const source = ts.createSourceFile('component.tsx', text, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
    const expressions = [];
    function visit(node) {
        const call = ts.isCallExpression(node) && /^(useFieldConfiguration|useTextTransform|useLookup|useCompoundLookup|entityForm\.configureField|entityForm\.removeValidation|textTransform)$/.test(node.expression.getText(source));
        const variable = ts.isVariableDeclaration(node) && /^(handleOnBlur|handleOnComplete|handleOKSearch|handleOnAddressFound|effectiveRequired|is_required)$/.test(node.name.getText(source));
        const attribute = ts.isJsxAttribute(node) && ['required', 'withAsterisk', 'validate'].includes(node.name.getText(source));
        if (call || variable || attribute) expressions.push(printer.printNode(ts.EmitHint.Unspecified, node, source));
        ts.forEachChild(node, visit);
    }
    visit(source);
    return expressions;
}

function ruleCounts(results) {
    const counts = {};
    for (const message of results.flatMap(result => result.messages)) {
        const key = `${message.severity}:${message.ruleId}`;
        counts[key] = (counts[key] ?? 0) + 1;
    }
    return counts;
}

function errorPropOrder(text) {
    const source = ts.createSourceFile('component.tsx', text, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
    const orders = [];
    function visit(node) {
        if (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) {
            const attributes = node.attributes.properties;
            const errorIndex = attributes.findIndex(attribute => ts.isJsxAttribute(attribute) && attribute.name.getText(source) === 'error');
            const inputIndex = attributes.findIndex(attribute => ts.isJsxSpreadAttribute(attribute) && attribute.expression.getText(source) === 'inputProps');
            if (errorIndex >= 0 && inputIndex >= 0) orders.push(errorIndex < inputIndex);
        }
        ts.forEachChild(node, visit);
    }
    visit(source);
    return orders;
}

(async () => {
    const eslint = new ESLint({ cwd });
    let originalErrors = 0;
    let currentErrors = 0;
    for (const relative of changed) {
        assert.equal(/\/Lookup[^/]*\/use[^/]+\.[tj]sx?$/.test(relative), false, `Lookup hook changed: ${relative}`);
        assert.equal(/\/Form\/useFieldConfiguration\.ts$/.test(relative), false, 'Field validation hook changed');
        const original = execFileSync(git, ['show', `HEAD:${relative}`], { cwd, encoding: 'utf8' });
        const filePath = path.resolve(cwd, relative.replace(/^MicroM\/micromlib\//, ''));
        const current = fs.readFileSync(filePath, 'utf8');
        assert.deepEqual(protectedExpressions(current), protectedExpressions(original), `${relative}: validation/transformation changed`);
        assert.deepEqual(errorPropOrder(current), errorPropOrder(original), `${relative}: validation error precedence changed`);
        const before = await eslint.lintText(original, { filePath });
        const after = await eslint.lintText(current, { filePath });
        const beforeCounts = ruleCounts(before);
        for (const [rule, count] of Object.entries(ruleCounts(after))) assert.ok(count <= (beforeCounts[rule] ?? 0), `${relative}: new lint issue ${rule}`);
        originalErrors += before.reduce((count, result) => count + result.errorCount, 0);
        currentErrors += after.reduce((count, result) => count + result.errorCount, 0);
    }
    for (const name of ['fieldUI.ts']) {
        const results = await eslint.lintFiles(path.join(cwd, 'src/UI/Core', name));
        assert.equal(results.reduce((count, result) => count + result.errorCount, 0), 0, `${name}: lint errors`);
    }
    console.log(`PASS ${changed.length} changed UI files preserve validation/transformation expressions; no lookup hooks changed`);
    console.log(`PASS lint baseline comparison: ${originalErrors} original errors, ${currentErrors} current errors; no new rule violations`);
})().catch(error => { console.error(error); process.exitCode = 1; });
