const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');

(async () => {
    const browser = await chromium.launch({ headless: true, executablePath: process.env.CHROMIUM_PATH });
    const page = await browser.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (['error', 'warning'].includes(message.type())) errors.push(message.text()); });
    const url = new URL('hierarchy.html', process.env.FIELD_UI_URL || 'http://127.0.0.1:1256/');
    const snapshot = () => page.evaluate(() => window.hierarchyFixture.snapshot());
    async function settled() {
        await page.waitForFunction(() => window.hierarchyFixture);
        let after = await snapshot();
        let stable = 0;
        for (let attempt = 0; attempt < 20 && stable < 3; attempt++) {
            await page.waitForTimeout(50);
            const next = await snapshot();
            stable = next.commits.length === after.commits.length ? stable + 1 : 0;
            after = next;
        }
        assert.equal(stable, 3, 'renders settle');
        assert.deepEqual(errors, [], 'no React warnings or browser errors');
        return after;
    }
    async function select(name, label) {
        const input = page.getByLabel(name, { exact: true });
        await input.waitFor();
        await page.waitForFunction(input => !input.readOnly, await input.elementHandle());
        await input.click();
        await page.getByRole('option', { name: label, exact: true }).click();
        return settled();
    }
    async function set(values) {
        await page.evaluate(values => window.hierarchyFixture.setValues(values), values);
        return settled();
    }
    try {
        await page.goto(url.href);
        await settled();
        let state = await select('campus', 'Campus One');
        assert.deepEqual(state.clears, [], 'empty descendants are not rewritten');
        assert.deepEqual(state.keys[1], { campus: 'c1', building: '' });
        state = await select('building', 'Building One');
        assert.equal(state.values.building, 'b1', 'child selection persists');
        await select('room', 'Room One');
        state = await select('campus', 'Campus Two');
        assert.deepEqual(state.values, { campus: 'c2', building: '', room: '' });
        assert.deepEqual(state.clears, ['building', 'room'], 'populated descendants cleared once');
        assert.ok(state.requests.some(request => request.name === 'building' && request.keys.campus === 'c2'), 'child lookup refreshed for new campus');
        await select('building', 'Building Two');
        await select('room', 'Room Two');
        state = await set({ building: 'other-building' });
        assert.equal(state.values.campus, 'c2');
        assert.equal(state.values.room, '');
        state = await set({ room: 'r2' });
        state = await set({ campus: 'c1', building: 'b1' });
        assert.deepEqual(state.values, { campus: 'c1', building: 'b1', room: '' }, 'batch preserves supplied hierarchy levels');
        state = await set({ campus: 'c2', building: 'b2', room: 'r2' });
        const clearCount = state.clears.length;
        const requestCount = state.requests.length;
        await page.evaluate(() => window.hierarchyFixture.rerender());
        state = await settled();
        assert.ok(state.commits.at(-1).sameKeys, 'inline arrays retain parent-key identity');
        assert.equal(state.clears.length, clearCount);
        assert.equal(state.requests.length, requestCount, 'unrelated render does not refetch');
        for (const values of [{ campus: 'c1', building: 'b1', room: 'r1' }, { campus: 'c2', building: 'b2', room: 'r2' }]) {
            await page.evaluate(() => window.hierarchyFixture.beginLoad());
            await settled();
            await page.evaluate(values => window.hierarchyFixture.finishLoad(values), values);
            state = await settled();
            assert.deepEqual(state.values, values, 'completed get preserves loaded hierarchy');
            assert.equal(state.clears.length, clearCount);
            assert.deepEqual(state.keys[2], values);
        }
        await page.goto(`${url.href}?defaults=1&mapped=1`);
        state = await settled();
        assert.deepEqual(state.keys[1], { lookupCampus: null, lookupBuilding: 'default-building' }, 'mapped names, explicit null and missing-field defaults');
        assert.deepEqual(state.clears, []);
        state = await set({ campus: 'c1', building: 'b1', room: 'r1' });
        assert.deepEqual(state.keys[2], { lookupCampus: 'c1', lookupBuilding: 'b1', lookupRoom: 'r1' });
        state = await set({ campus: 'c2' });
        assert.deepEqual(state.keys[2], { lookupCampus: 'c2', lookupBuilding: '', lookupRoom: '' });
        assert.deepEqual(errors, []);
        console.log('PASS hierarchy: real React/Mantine Strict Mode, cascading lookups, bounded renders, stable keys, batch updates, reloads, defaults/null and mapped keys');
    } catch (error) {
        const state = await snapshot();
        console.error('Hierarchy fixture diagnostics:', { values: state.values, keys: state.keys, clears: state.clears, commits: state.commits.length, errors });
        throw error;
    } finally {
        await browser.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
