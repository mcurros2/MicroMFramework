const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

function loadNavigationGuards() {
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src/UI/Router/NavigationGuards.ts'), 'utf8');
    vm.runInNewContext(ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
    }).outputText, { exports });
    return exports;
}

function loadEntityFormNavigationProtection(useConfirmNavigation) {
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src/UI/Form/useEntityFormNavigationProtection.ts'), 'utf8');
    const guards = loadNavigationGuards();
    vm.runInNewContext(ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
    }).outputText, {
        exports,
        require: moduleName => moduleName.endsWith('NavigationGuards')
            ? guards
            : { useConfirmNavigation },
    });
    return exports;
}

test('confirm acts as disabled only when both standard buttons are hidden without custom buttons', () => {
    const { resolveNavigationProtectionMode } = loadNavigationGuards();

    for (const buttons of [undefined, null, false, '', 0]) {
        assert.equal(resolveNavigationProtectionMode('confirm', false, false, buttons), 'disabled');
    }

    assert.equal(resolveNavigationProtectionMode('confirm', false, false, 'custom buttons'), 'confirm');
    assert.equal(resolveNavigationProtectionMode('confirm', true, false), 'confirm');
    assert.equal(resolveNavigationProtectionMode('confirm', false, true), 'confirm');
    assert.equal(resolveNavigationProtectionMode('confirm', undefined, undefined), 'confirm');

    for (const mode of ['save', 'allways', 'disabled', undefined]) {
        assert.equal(resolveNavigationProtectionMode(mode, false, false), mode);
    }
});

test('entity form presentation controls the mode passed to the navigation guard', () => {
    let receivedOptions;
    const { useEntityFormNavigationProtection } = loadEntityFormNavigationProtection(options => { receivedOptions = options; });
    const navigationGuard = {
        mode: 'confirm',
        hasUnsavedChanges: () => true,
        onSave: async () => true,
    };
    const formAPI = { navigationGuard };

    useEntityFormNavigationProtection(formAPI, { showCancel: false, showOK: false });
    assert.equal(receivedOptions.mode, 'disabled');
    assert.equal(receivedOptions.hasUnsavedChanges, navigationGuard.hasUnsavedChanges);

    useEntityFormNavigationProtection(formAPI, { showCancel: false, showOK: false, buttons: 'custom buttons' });
    assert.equal(receivedOptions.mode, 'confirm');
});

test('disabled registers no guards, click or unload handlers; mode changes clean up', () => {
    const effects = [];
    const registrations = new Set();
    const eventTarget = {
        addEventListener: (_, handler) => registrations.add(handler),
        removeEventListener: (_, handler) => registrations.delete(handler),
    };
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src/UI/Router/useConfirmNavigation.tsx'), 'utf8');
    const register = guard => { registrations.add(guard); return () => registrations.delete(guard); };
    vm.runInNewContext(ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX },
    }).outputText, {
        exports, window: eventTarget, document: eventTarget,
        require: () => ({
            useRef: current => ({ current }), useCallback: fn => fn,
            useEffect: fn => effects.push(fn), useModal: () => ({}),
            useModalScope: () => undefined,
            registerLocalNavigationGuard: register,
        }),
    });
    function render(mode) {
        effects.length = 0;
        exports.useConfirmNavigation({ mode, hasUnsavedChanges: () => true, onSave: async () => true });
        const cleanups = effects.map(fn => fn()).filter(Boolean);
        return () => cleanups.forEach(fn => fn());
    }
    let cleanup = render('disabled');
    assert.equal(registrations.size, 0);
    cleanup();
    cleanup = render('confirm');
    assert.equal(registrations.size, 3);
    cleanup();
    cleanup = render('disabled');
    assert.equal(registrations.size, 0);
    cleanup();
});
