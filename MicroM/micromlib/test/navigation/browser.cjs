// Start: npx parcel test/navigation/index.html --port 1255 --host 127.0.0.1
// Run with Playwright installed, or set PLAYWRIGHT_MODULE to its module directory.
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');

(async () => {
    const browser = await chromium.launch({ headless: true, executablePath: process.env.CHROMIUM_PATH });
    const errors = [];
    const page = await browser.newPage();
    page.on('pageerror', error => errors.push(error.message));
    page.setDefaultTimeout(6000);
    const button = name => page.getByRole('button', { name, exact: true });
    const form = depth => page.getByText(`Form ${depth}`, { exact: true });
    const back = () => page.evaluate(() => history.back());
    const route = () => page.getByText('Route: /orders', { exact: true }).waitFor();
    async function fresh() {
        await page.goto('http://127.0.0.1:1255/');
        await button('Orders route').click();
        await route();
        await button('Open form').click();
        await form(1).waitFor();
    }
    async function mode(value) {
        await page.getByLabel('Protection 1', { exact: true }).click();
        await page.getByRole('option', { name: value, exact: true }).click();
    }
    try {
        await fresh();
        await page.getByLabel('Value 1', { exact: true }).fill('parent');
        await button('Open nested').click();
        await page.getByLabel('Value 2', { exact: true }).fill('child');
        await back();
        await button('Stay').click();
        await button('Stay').waitFor({ state: 'hidden' });
        await form(2).waitFor();
        assert.equal(await page.getByLabel('Value 2').inputValue(), 'child');
        await back();
        await button('Leave without saving').click();
        await form(2).waitFor({ state: 'hidden' });
        await form(1).waitFor();
        await route();
        assert.equal(await page.getByLabel('Value 1').inputValue(), 'parent');
        await back();
        await button('Save and leave').click();
        await form(1).waitFor({ state: 'hidden' });
        await route();
        await page.evaluate(() => history.forward());
        await page.waitForFunction(() => history.state?.__micromModal?.position === 0);
        assert.equal(await form(1).count(), 0);
        await back();
        await page.getByText('Route: /', { exact: true }).waitFor();
        console.log('PASS nested Back / Stay / Leave / Save / Forward / underlying history');

        await fresh();
        await page.getByLabel('Value 1').fill('dirty');
        await button('Failure: false').click();
        await page.getByTitle('Close', { exact: true }).click();
        await button('Save and leave').click();
        await page.getByText('Calls: 1', { exact: true }).waitFor();
        await form(1).waitFor();
        await back();
        await button('Leave without saving').click();
        await page.getByText('Calls: 2', { exact: true }).waitFor();
        await form(1).waitFor();
        await button('Failure: true').click();
        await page.keyboard.press('Escape');
        await button('Leave without saving').click();
        await form(1).waitFor({ state: 'hidden' });
        await route();
        console.log('PASS X / Escape / failed Save and Cancel preserve modal');

        for (const option of ['disabled', 'save']) {
            await fresh();
            await page.getByLabel('Value 1').fill('dirty');
            await mode(option);
            await back();
            await form(1).waitFor({ state: 'hidden' });
            assert.equal(await button('Stay').count(), 0);
            await route();
        }
        await fresh();
        await page.getByLabel('Value 1').fill('dirty');
        await mode('disabled');
        await mode('confirm');
        await back();
        await button('Stay').click();
        await button('Stay').waitFor({ state: 'hidden' });
        await button('View: false').click();
        await back();
        await form(1).waitFor({ state: 'hidden' });
        console.log('PASS disabled / save / dynamic enable / view');

        await fresh();
        await page.getByLabel('Value 1').fill('dirty');
        await page.evaluate(() => { location.hash = '/other'; });
        await button('Stay').click();
        await button('Stay').waitFor({ state: 'hidden' });
        await route();
        assert.ok(page.url().endsWith('#/orders'));
        await button('Explicit close').click();
        await form(1).waitFor({ state: 'hidden' });
        await back();
        await page.getByText('Route: /', { exact: true }).waitFor();
        console.log('PASS raw hash interception / explicit close removes history entry');

        await fresh();
        await mode('allways');
        await back();
        await button('Stay').waitFor();
        await back();
        await back();
        assert.equal(await button('Stay').count(), 1);
        await button('Stay').click();
        await button('Stay').waitFor({ state: 'hidden' });
        await form(1).waitFor();
        await button('Explicit close').click();
        await form(1).waitFor({ state: 'hidden' });
        await button('Open async').click();
        await form(1).waitFor();
        await page.getByLabel('Value 1').fill('async dirty');
        await page.mouse.click(5, 5);
        await button('Stay').click();
        await button('Stay').waitFor({ state: 'hidden' });
        await mode('disabled');
        const dialogs = [];
        const onDialog = async dialog => { dialogs.push(dialog.type()); await dialog.dismiss(); };
        page.on('dialog', onDialog);
        await page.reload();
        await button('Open form').waitFor();
        assert.deepEqual(dialogs, []);
        page.off('dialog', onDialog);
        console.log('PASS allways / repeated Back / async content / outside click / disabled reload');

        assert.deepEqual(errors, []);
    } catch (error) {
        console.error('DOM:', await page.locator('body').innerText());
        console.error('URL:', page.url(), await page.evaluate(() => history.state));
        throw error;
    } finally {
        if (errors.length) console.error('Browser errors:', errors);
        await browser.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
