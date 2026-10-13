// Start: node node_modules/parcel/lib/bin.js test/fields/index.html --port 1256 --host 127.0.0.1
// Run: node test/fields/browser.cjs (or set PLAYWRIGHT_MODULE to an installed module directory).
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');

const kinds = [
    'TextField', 'TextAreaField', 'EmailField', 'UrlField', 'CUITField', 'PhoneField',
    'NumberField', 'MoneyRangeField', 'BytesRangeField', 'PasswordField', 'DateInputField',
    'TimeField', 'PinField', 'CheckboxField', 'SwitchField', 'RadioGroupField', 'WeekPickerField',
    'LookupSelect', 'LookupMultiSelect', 'Lookup', 'CompoundLookup', 'AddressAutocomplete', 'AddressLookup',
];

(async () => {
    const browser = await chromium.launch({ headless: true, executablePath: process.env.CHROMIUM_PATH });
    const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.setDefaultTimeout(8000);
    page.setDefaultNavigationTimeout(30000);
    // Local Google API stubs avoid any network requests or API key requirement.
    await page.addInitScript(() => {
        window.google = { maps: {
            Map: class {}, Geocoder: class {},
            places: {
                AutocompleteSessionToken: class {},
                AutocompleteService: class {
                    getPlacePredictions(_request, callback) { callback([], 'ZERO_RESULTS'); }
                },
                PlacesService: class {},
                PlacesServiceStatus: { OK: 'OK', ZERO_RESULTS: 'ZERO_RESULTS', NOT_FOUND: 'NOT_FOUND' },
            },
        } };
        window.pickerCalls = 0;
        HTMLInputElement.prototype.showPicker = function () { window.pickerCalls++; };
    });
    const field = () => page.getByTestId('field');
    const inputs = () => field().locator('input:not([type="hidden"]), textarea');
    const value = () => page.locator('#value').textContent();
    async function open(kind, options = {}) {
        await page.goto(`${process.env.FIELD_UI_URL || 'http://127.0.0.1:1256/'}?${new URLSearchParams({ kind, ...options })}`, { waitUntil: 'domcontentloaded' });
        await field().waitFor({ timeout: 30000 });
        if (kind !== 'WeekPickerField') await inputs().first().waitFor({ state: 'attached' });
        // Loaded select data is asynchronous, even with the local fixture API.
        if (['LookupSelect', 'LookupMultiSelect'].includes(kind)) await page.waitForFunction(() => !document.querySelector('[data-testid="field"] input')?.readOnly || new URLSearchParams(location.search).get('readonly') === 'true' || new URLSearchParams(location.search).get('mode') === 'view' || new URLSearchParams(location.search).has('flag') || new URLSearchParams(location.search).get('loading') !== 'false');
    }
    async function hasWidth(property, expected, kind) {
        await page.waitForFunction(({ property, expected }) => {
            const fieldset = document.querySelector('[data-testid="field"] fieldset');
            const root = fieldset?.firstElementChild;
            return root && getComputedStyle(root)[property] === expected;
        }, { property, expected });
        assert.equal(await field().locator('fieldset > *').first().evaluate((node, property) => getComputedStyle(node)[property], property), expected, kind);
    }
    async function marker(kind) {
        if (kind === 'RadioGroupField') {
            assert.equal(await field().locator('input[data-autofocus]').count(), 0, 'radio children have no injected markers');
            return;
        }
        if (kind === 'PinField') {
            assert.equal(await field().locator('[data-autofocus]').count(), 0, 'PIN has no injected marker');
            if (new URLSearchParams(page.url().split('?')[1]).get('loading') !== 'true') {
                assert.equal(await inputs().first().evaluate(node => node === document.activeElement), true, 'native PIN mount autofocus');
            }
            return;
        }
        const target = field().locator('[data-autofocus]');
        assert.equal(await target.count(), 1, `${kind}: one autofocus marker`);
        assert.equal(await target.evaluate(node => ['INPUT', 'TEXTAREA', 'BUTTON'].includes(node.tagName)), true, `${kind}: native focus target`);
        await page.waitForFunction(() => document.activeElement?.hasAttribute('data-autofocus'));
        if (kind === 'RadioGroupField') assert.equal(await target.getAttribute('value'), 'b');
    }
    try {
        const start = process.env.FIELD_UI_START ? kinds.indexOf(process.env.FIELD_UI_START) : 0;
        assert.ok(start >= 0, 'FIELD_UI_START must name a supported field');
        for (const kind of kinds.slice(start)) {
            await open(kind, { focus: 'false', maw: '42rem', miw: '11rem', max: 'xs', min: 'sm' });
            assert.equal(await field().locator('[data-autofocus]').count(), 0, `${kind}: false marker absent`);
            await hasWidth('maxWidth', '672px', kind);
            await hasWidth('minWidth', '176px', kind);
            await open(kind, { focus: 'autoFocusOnAdd', loading: 'false' });
            await marker(kind);
            await open(kind, { focus: 'autoFocusOnEdit', mode: 'edit', loading: 'false' });
            await marker(kind);
            await open(kind, { focus: 'autoFocusOnEdit', mode: 'view', loading: 'false' });
            if (['CheckboxField', 'SwitchField', 'RadioGroupField', 'PinField'].includes(kind)) {
                assert.equal(await inputs().first().isDisabled(), true, kind);
                assert.equal(await inputs().first().evaluate(node => node === document.activeElement), false, kind);
            } else await marker(kind);
            await open(kind, { focus: 'autoFocusOnEdit', mode: 'edit', loading: 'true' });
            assert.equal(await field().locator('[data-autofocus]').count(), 0);
            await page.evaluate(() => window.fieldFixture.finishLoading());
            await marker(kind);
            console.log(`PASS ${kind}: width overrides, false marker, add/edit/view/loading focus`);
        }

        for (const kind of kinds.filter(kind => kind !== 'WeekPickerField')) {
            for (const options of [{ readonly: 'true', disabled: 'false' }, { mode: 'view', readonly: 'false' }, { mode: 'edit', flag: 'pk' }, { flag: 'autoNum' }]) {
                await open(kind, options);
                if (['CheckboxField', 'SwitchField', 'RadioGroupField', 'PinField'].includes(kind)) {
                    const before = await value();
                    for (const input of await inputs().all()) {
                        assert.equal(await input.isDisabled(), true, kind);
                        await input.evaluate(node => node.focus());
                        assert.equal(await input.evaluate(node => node === document.activeElement), false, kind);
                    }
                    if (kind !== 'PinField') await field().getByText(kind === 'RadioGroupField' ? 'Alpha' : 'Field', { exact: true }).click({ force: true });
                    await page.keyboard.press('Space');
                    await page.keyboard.press('ArrowRight');
                    assert.equal(await value(), before, kind);
                } else {
                    assert.equal(await inputs().first().evaluate(node => node.readOnly), true, `${kind}: native readOnly`);
                    const before = await value();
                    await inputs().first().focus();
                    await page.keyboard.type('x');
                    await page.keyboard.press('ArrowUp');
                    await page.keyboard.press('Backspace');
                    assert.equal(await value(), before, `${kind}: readOnly typing/stepping/delete`);
                }
            }
            await open(kind, { mode: 'edit', flag: 'pk', readonly: 'false' });
            if (['RadioGroupField', 'CheckboxField', 'SwitchField', 'PinField'].includes(kind)) assert.equal(await inputs().first().isDisabled(), false, kind);
            if (!['RadioGroupField', 'CheckboxField', 'SwitchField', 'PinField'].includes(kind)) await page.waitForFunction(() => document.querySelector('[data-testid="field"] input, [data-testid="field"] textarea')?.readOnly === false);
            console.log(`PASS ${kind}: explicit/implicit readOnly and view override`);
        }

        for (const kind of ['CheckboxField', 'SwitchField', 'RadioGroupField']) {
            await open(kind, { disabled: 'true' });
            for (const input of await inputs().all()) assert.equal(await input.isDisabled(), true, kind);
            if (kind === 'RadioGroupField') {
                const styles = await field().locator('fieldset fieldset').evaluate(node => ({ borderWidth: node.style.borderWidth, margin: node.style.margin, padding: node.style.padding, minInlineSize: node.style.minInlineSize }));
                assert.deepEqual(styles, { borderWidth: '0px', margin: '0px', padding: '0px', minInlineSize: 'unset' });
            }
            await open(kind, { loading: 'true' });
            for (const input of await inputs().all()) assert.equal(await input.isDisabled(), true, `${kind}: loading`);
            await page.evaluate(() => window.fieldFixture.finishLoading());
            await page.waitForFunction(() => !document.querySelector('[data-testid="field"] input')?.matches(':disabled'));
            await open(kind);
            const before = await value();
            await field().getByText(kind === 'RadioGroupField' ? 'Alpha' : 'Field', { exact: true }).click();
            assert.notEqual(await value(), before, `${kind}: editable changes still work`);
        }

        await open('PinField', { disabled: 'true' });
        for (const input of await inputs().all()) assert.equal(await input.isDisabled(), true);
        await open('PinField', { value: '' });
        await inputs().first().focus();
        await page.keyboard.type('5678');
        assert.equal(await value(), '"5678"', 'editable PIN typing');
        await open('PinField', { value: '' });
        await inputs().first().focus();
        await inputs().first().evaluate(node => {
            const data = new DataTransfer(); data.setData('Text', '9876');
            node.dispatchEvent(new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: data }));
        });
        assert.equal(await value(), '"9876"', 'editable PIN paste');

        for (const options of [{ readonly: 'true' }, { mode: 'view', readonly: 'false' }, { mode: 'edit', flag: 'pk' }, { flag: 'autoNum' }, { legacy: 'true' }]) {
            await open('WeekPickerField', options);
            const before = await value();
            await field().locator('tbody td [data-weekend]').first().click();
            assert.equal(await value(), before, 'static week cannot change');
        }
        await open('WeekPickerField', { legacy: 'true', readonly: 'false' });
        const weekBefore = await value();
        await field().locator('tbody td [data-weekend]').first().click();
        assert.notEqual(await value(), weekBefore, 'canonical false overrides legacy true');

        for (const kind of ['Lookup', 'CompoundLookup']) {
            await open(kind, { legacy: 'true', readonly: 'false' });
            assert.equal(await inputs().first().evaluate(node => node.readOnly), false);
            await open(kind, { legacy: 'true' });
            assert.equal(await inputs().first().evaluate(node => node.readOnly), true);
        }

        for (const kind of ['TimeField', 'LookupSelect', 'Lookup', 'CompoundLookup', 'AddressLookup']) {
            await open(kind, { readonly: 'true' });
            if (kind === 'TimeField') {
                await page.evaluate(() => window.fieldFixture.showPicker());
                assert.equal(await page.evaluate(() => window.pickerCalls), 1, 'imperative picker forwards unchanged');
            }
            for (const button of await field().locator('button').all()) {
                // Select chevrons are not action buttons; component-owned actions are disabled.
                if (kind === 'LookupSelect' && !(await button.textContent()).includes('Edit')) continue;
                assert.equal(await button.isDisabled(), true, `${kind}: action disabled`);
            }
        }
        await open('TimeField');
        await field().locator('button').click();
        assert.equal(await page.evaluate(() => window.pickerCalls), 1, 'clock button forwards picker');
        await page.evaluate(() => window.fieldFixture.showPicker());
        assert.equal(await page.evaluate(() => window.pickerCalls), 2);

        for (const kind of ['LookupSelect', 'LookupMultiSelect']) {
            await open(kind);
            await page.evaluate(() => window.fieldFixture.setValidationError());
            await field().getByText('Fixture validation', { exact: true }).waitFor();
        }

        await open('TextField', { readonly: 'true', value: '  ABC  ' });
        await inputs().first().focus();
        await page.locator('#outside').click();
        assert.equal(await value(), '"ABC"', 'readOnly blur trimming remains unchanged');
        assert.equal(await page.locator('#blur').textContent(), '1', 'caller blur handler remains active');
        await open('TextAreaField', { readonly: 'true', value: '  ABC  ' });
        await inputs().first().focus();
        await page.locator('#outside').click();
        assert.equal(await value(), '"ABC"', 'textarea transformation remains unchanged');

        for (const options of [{}, { flag: 'autoNum' }, { mode: 'edit', flag: 'pk' }]) {
            await open('AutoForm', options);
            await marker('AutoForm');
        }
        await open('TextField', { maw: '0', miw: '0', max: 'sm', min: 'sm' });
        await hasWidth('maxWidth', '0px', 'TextField');
        await hasWidth('minWidth', '0px', 'TextField');
        await open('TextField', { responsive: 'true', max: 'xs' });
        await hasWidth('maxWidth', '672px', 'TextField');
        await page.setViewportSize({ width: 500, height: 900 });
        await hasWidth('maxWidth', '336px', 'TextField');
        await page.setViewportSize({ width: 1280, height: 900 });
        for (const kind of ['PinField', 'RadioGroupField', 'WeekPickerField']) {
            await open(kind, { entityForm: 'true', focus: 'autoFocusOnEdit', mode: 'edit', loading: 'true' });
            await page.evaluate(() => window.fieldFixture.finishLoading());
            if (kind === 'WeekPickerField') await marker(kind);
            else {
                assert.equal(await field().locator('input[data-autofocus]').count(), 0);
                for (const input of await inputs().all()) assert.equal(await input.isDisabled(), false);
                await page.keyboard.press('Tab');
                assert.equal(await field().locator('input').evaluateAll(nodes => nodes.some(node => node === document.activeElement)), true, 'native child tab focus');
            }
        }
        assert.deepEqual(errors, []);
        console.log('PASS native interaction guards, disabled PIN and editable typing/paste, static weeks, action buttons, aliases, unchanged transforms, AutoForm, responsive/zero widths, EntityForm FocusTrap');
    } catch (error) {
        console.error(error);
        console.error('URL:', page.url());
        console.error('DOM:', await page.locator('body').innerHTML({ timeout: 1000 }).catch(() => 'Page body unavailable'));
        console.error('Browser errors:', errors);
        throw error;
    } finally {
        await browser.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
