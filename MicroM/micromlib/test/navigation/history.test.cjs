const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');
function load(file) {
    const exports = {};
    const source = fs.readFileSync(path.join(__dirname, '../../src/UI/Router', file), 'utf8');
    vm.runInNewContext(ts.transpileModule(source, {
        compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
    }).outputText, { exports, console });
    return exports;
}
const tick = () => new Promise(resolve => setTimeout(resolve, 10));
async function until(predicate) {
    for (let attempt = 0; attempt < 100; attempt++) {
        if (predicate()) return;
        await tick();
    }
    assert.ok(predicate(), 'history operation did not settle');
}

function fakeWindow() {
    let cursor = 0;
    const entries = [{ state: { existing: true }, url: 'https://test/#/orders' }];
    const listeners = new Set();
    const host = {
        setTimeout,
        location: { get href() { return entries[cursor].url; } },
        addEventListener: (_, fn) => listeners.add(fn),
        removeEventListener: (_, fn) => listeners.delete(fn),
        history: {
            get state() { return entries[cursor].state; },
            replaceState(state, _, url) { entries[cursor] = { state, url }; },
            pushState(state, _, url) { entries.splice(++cursor, entries.length, { state, url }); },
            go(delta) {
                const next = cursor + delta;
                if (next < 0 || next >= entries.length) return;
                setTimeout(() => { cursor = next; for (const fn of listeners) fn(); }, 0);
            },
        },
        get cursor() { return cursor; },
    };
    return host;
}

test('scope registration retains the parent and never checks it for a child close', async () => {
    const { registerLocalNavigationGuard, canNavigateLocally } = load('NavigationGuards.ts');
    const calls = [];
    registerLocalNavigationGuard(() => { calls.push('page'); return false; });
    const unregister = registerLocalNavigationGuard(() => { calls.push('modal'); return true; }, 'modal');
    const intent = { currentRoute: '/', nextRoute: '/next' };
    assert.equal(await canNavigateLocally(intent, 'modal'), true);
    assert.deepEqual(calls, ['modal']);
    unregister();
    assert.equal(await canNavigateLocally(intent), false);
    assert.deepEqual(calls, ['modal', 'page']);
});

test('nested Back, Stay, programmatic close and Forward preserve the underlying route', async () => {
    const { ModalHistory } = load('ModalNavigation.ts');
    const host = fakeWindow();
    const stack = [];
    let allow = false;
    let prompts = 0;
    const history = new ModalHistory(async () => {
        prompts++;
        if (allow) await history.close(stack.pop());
    }, host);
    for (const id of ['a', 'b', 'c']) { stack.push(id); await history.open(id); }
    assert.equal(host.cursor, 3);
    assert.equal(host.history.state.existing, true);
    host.history.go(-1);
    await until(() => prompts === 1 && host.cursor === 3);
    assert.equal(prompts, 1);
    assert.equal(host.cursor, 3);
    assert.equal(stack.length, 3);
    allow = true;
    host.history.go(-1);
    await until(() => host.cursor === 2 && stack.length === 2);
    assert.equal(host.cursor, 2);
    assert.deepEqual(stack, ['a', 'b']);
    host.history.go(1);
    await tick(); await tick();
    assert.equal(host.cursor, 2);
    assert.equal(prompts, 2);
    await history.close(stack.pop());
    assert.equal(host.cursor, 1);
    host.history.go(-1);
    await until(() => host.cursor === 0 && stack.length === 0);
    assert.equal(host.cursor, 0);
    assert.deepEqual(stack, []);
    assert.equal(host.location.href, 'https://test/#/orders');
    history.dispose();
});

test('closing a covered modal skips its dead entry when the child closes', async () => {
    const { ModalHistory } = load('ModalNavigation.ts');
    const host = fakeWindow();
    const history = new ModalHistory(async () => {}, host);
    await history.open('parent'); await history.open('child');
    await history.close('parent');
    assert.equal(host.cursor, 2);
    await history.close('child');
    assert.equal(host.cursor, 0);
    await history.close('child');
    assert.equal(host.cursor, 0);
    history.dispose();
});

test('simultaneous closes and duplicate closes leave no stale Back entries', async () => {
    const { ModalHistory } = load('ModalNavigation.ts');
    const host = fakeWindow();
    const history = new ModalHistory(async () => {}, host);
    await history.open('parent'); await history.open('child');
    await Promise.all([history.close('child'), history.close('parent'), history.close('child')]);
    assert.equal(host.cursor, 0);
    history.dispose();
});
