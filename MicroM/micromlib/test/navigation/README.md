# Navigation regression checks

## Disabled

`navigationProtection="disabled"` opts out of local guards, external link
interception and unload protection. It does not change explicit Save/Cancel.
The option is available through `FormOptions`, `EntityFormModal` and `useOpenForm`.
Changing the mode removes the previous listeners and any pending confirmation.

## Managed modals

Forms inside `ModalsManager` register guards in their modal's scope. Back, X,
Escape and enabled outside clicks request closure of the top modal only.
Programmatic `close()` retains its immediate semantics; `close(id)` targets a
specific modal and repeated closes share the same completion promise.

Opening a normal modal adds a same-URL history entry with an internal identifier.
It does not serialize data or make the modal addressable by URL. Back restores
the entry while protection is pending, then consumes it only on successful close.
Closed entries never recreate forms through Forward. Confirmation dialogs use
`history: false` and an explicit `id`; they close before save/cancel callbacks run.

## Run

From `MicroM/micromlib`:

```powershell
node --test test/navigation/*.test.cjs
npx parcel test/navigation/index.html --port 1255 --host 127.0.0.1
```

In another terminal, with Playwright and Chromium available:

```powershell
node test/navigation/browser.cjs
```

Optionally set `PLAYWRIGHT_MODULE` to an existing Playwright module directory
and `CHROMIUM_PATH` to an installed Chromium executable. The test page uses
simulated save/cancel operations and does not connect to a backend.

Browser checks cover nested modals, Stay/Save/Leave, failed operations, repeated
Back, Forward, X/Escape/outside clicks, async content, mode changes, disabled
reload and preservation of the underlying route and history.
