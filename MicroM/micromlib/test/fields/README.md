# Form field UI regression checks

Build the isolated fixture from its browser-only package, starting in `micromlib`:

```powershell
Push-Location test/fields
node ../../node_modules/parcel/lib/bin.js build index.html --target fixture --no-optimize --no-scope-hoist --dist-dir ../../dist/field-ui-test --no-cache
Pop-Location
node test/fields/serve.cjs
```

Run the browser matrix with an installed Playwright package:

```powershell
node test/fields/browser.cjs
```

For the focused `useHierarchyKeys` regression, build the development React bundle
so Strict Mode checks and console warnings stay enabled, start the same static
server, and run:

```powershell
Push-Location test/fields
$env:NODE_ENV = 'development'
node ../../node_modules/parcel/lib/bin.js build hierarchy.html --target fixture --no-optimize --dist-dir ../../dist/field-ui-test --no-cache
Remove-Item Env:NODE_ENV
Pop-Location
node test/fields/hierarchy.cjs
```

This fixture runs real React and Mantine in Strict Mode with three cascading
`LookupSelect` controls and local lookup responses. It checks that renders settle,
empty descendants are not rewritten, selections cascade correctly, unrelated
renders preserve parent keys, and record loads, mapped keys, defaults, and explicit
null values are preserved. React console warnings and browser errors fail the test.

If Playwright is installed elsewhere, set `PLAYWRIGHT_MODULE` to its module directory. Optional `CHROMIUM_PATH` and `FIELD_UI_URL` override the browser executable and fixture URL.

The static server avoids hot-reload timing during the navigation matrix. For interactive development, Parcel can also serve the page directly. `FIELD_UI_START` optionally resumes the width/focus phase at a named component while retaining all interaction checks.

The fixture uses local lookup responses and browser Google API stubs; no backend or Google API key is needed. To inspect it manually, open `?kind=TextField&mode=edit&focus=autoFocusOnEdit`, replacing the component name as needed. Address fields require the Google stubs supplied by the browser test.

Checks cover all entity-bound input implementations and wrappers, width overrides, focus modes and loading transitions, read-only UI, legacy aliases, AutoForm focus, and unchanged blur transformations. Validation and lookup hooks are intentionally unchanged.

Run resolver and existing data tests separately:

```powershell
node --test test/data/*.test.cjs
```

Before committing, `node test/fields/preservation.cjs` compares protected validation/transformation expressions and lint diagnostics against HEAD. Set `GIT_EXECUTABLE` if Git is not on PATH. This check deliberately rejects changes to lookup hooks and field validation configuration.
