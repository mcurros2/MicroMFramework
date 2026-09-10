const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

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
