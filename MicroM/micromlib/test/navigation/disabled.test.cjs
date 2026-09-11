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

test('navigation protection resolves the form mode and falls back to default', () => {
    const { DefaultNavigationProtection, resolveNavigationProtectionMode } = loadNavigationGuards();

    assert.equal(resolveNavigationProtectionMode(undefined, 'view'), 'disabled');
    assert.equal(resolveNavigationProtectionMode(undefined, 'add'), 'confirm');
    assert.equal(resolveNavigationProtectionMode(undefined, 'edit'), 'confirm');
    assert.deepEqual({ ...DefaultNavigationProtection }, { view: 'disabled', default: 'confirm' });

    const protection = { add: 'confirm', edit: 'allways', default: 'disabled' };
    assert.equal(resolveNavigationProtectionMode(protection, 'add'), 'confirm');
    assert.equal(resolveNavigationProtectionMode(protection, 'edit'), 'allways');
    assert.equal(resolveNavigationProtectionMode(protection, 'view'), 'disabled');
});

test('entity form presentation controls the mode passed to the navigation guard', () => {
    let receivedOptions;
    const { useEntityFormNavigationProtection } = loadEntityFormNavigationProtection(options => { receivedOptions = options; });
    const { resolveNavigationProtectionMode } = loadNavigationGuards();
    const navigationGuard = {
        mode: resolveNavigationProtectionMode(undefined, 'add'),
        hasUnsavedChanges: () => true,
        onSave: async () => true,
    };
    const formAPI = { navigationGuard };

    useEntityFormNavigationProtection(formAPI, { showCancel: false, showOK: false });
    assert.equal(receivedOptions.mode, 'disabled');
    assert.equal(receivedOptions.hasUnsavedChanges, navigationGuard.hasUnsavedChanges);

    useEntityFormNavigationProtection(formAPI, { showCancel: false, showOK: false, buttons: 'custom buttons' });
    assert.equal(receivedOptions.mode, 'confirm');

    useEntityFormNavigationProtection(formAPI, { showCancel: true, showOK: false });
    assert.equal(receivedOptions.mode, 'confirm');

    for (const mode of ['save', 'allways', 'disabled']) {
        useEntityFormNavigationProtection({ navigationGuard: { ...navigationGuard, mode } }, { showCancel: false, showOK: false });
        assert.equal(receivedOptions.mode, mode);
    }
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
