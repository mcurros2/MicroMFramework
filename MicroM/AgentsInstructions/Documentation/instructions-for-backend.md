# MicroMFramework Documentation Instructions

This file defines the iterative documentation workflow for documenting the backend of MicroMFramework using DocFX. **This file contains the instructions to be followed by agents. Sample files are only templates.**

---

## Documentation Files & Locations

- **Instructions (this file & samples):** `AgentsInstructions/Documentation/`
  - Instructions for agents live here.
  - Sample files (templates only, do not edit):
    - [`docs-state-backend-sample.md`](docs-state-backend-sample.md) → Template for tracking documentation state.
    - [`iteration-summary-backend-sample.md`](iteration-summary-backend-sample.md) → Template for iteration summaries.

- **Progress tracking (results created by AI agents):** `MicroM/Documentation-Progress/Backend/`
  - `docs-state-backend.md` → Maintained record of documentation coverage (namespaces & tutorials).
  - `iteration-summary.md` → Iteration progress and results log.
  - `iteration-notes.md` → Additional notes, context overflow, or long details.

- **Backend documentation site (source & output):** `MicroM/Documentation/Backend/`
  - Must contain an `index.md` at root.
  - Each namespace must have its own folder with its own `index.md`.
  - Sub-folders may be used if a namespace is too large.
  - Generated output (`_site`) must remain inside this directory. IT SHOULD BE INCLUDED IN GIT AND COMMITTED

---

## Documentation State & Iteration Summary Guidelines

Both `docs-state-backend.md` and `iteration-summary.md` follow the same principles:

- **docs-state-backend.md**
  - Organized by namespace and tutorial.
  - Notes coverage of XML comments, missing pages, and incomplete docs.
  - Always updated after a baseline verification run.
  - Use [`docs-state-backend-sample.md`](docs-state-backend-sample.md) as the template.

- **iteration-summary.md**
  - For each iteration, the file must contain structured blocks:
    - **Plan block**: Intended tasks (with file paths & namespaces).
    - **Execution results**: For each task, record the execution outcome:
      - `Success`: Completed as expected.
      - `Warning`: Completed with minor issues (e.g., missing XML comments).
      - `Error`: Expected but incomplete (e.g., page not linked in `index.md`).
      - `Failure`: Critical (e.g., docfx build failed, site not generated).
    - **Verification results**: For each task, confirm if the expected output exists and works.
      - Indicate `(success)` or `(failure)`.
      - If verification fails, explicitly state the reason (e.g., *“missing XML comments”*, *“page not linked in index.md”*).
      - Explicitly classify each failure as Warning / Error / Failure.
    - **Issues encountered**: List missing namespaces, incomplete docs, broken links.
    - **Forward tasks**: New or deferred work for future iterations, clearly marked.
  - Use [`iteration-summary-backend-sample.md`](iteration-summary-backend-sample.md) as the template.

Both files should cross-reference each other: `docs-state-backend.md` provides the coverage baseline, while `iteration-summary.md` records incremental progress.

**Samples are templates only. Agents must not update them.**

---

## Baseline Phase (Verification Pass)

The **baseline phase** establishes the current state of documentation for a namespace or for the backend overall.

- Run this phase:
  - At the very start (Iteration 0).
  - Each time a namespace is completed, to verify and correct current status.

### Steps
1. **Delete** `docs-state-backend.md`, `iteration-summary.md`, and `iteration-notes.md`.
   - Start fresh to ensure accurate baseline.
   - Use [`docs-state-backend-sample.md`](docs-state-backend-sample.md) as a template.
   - Document namespaces, tutorials, and coverage of XML comments.
   - Include any known gaps or issues.
2. Record gaps in `docs-state-backend.md`.
   - Ensure all namespaces are documented and mark the documentation state for each one.
   - Ensure all public APIs have XML comments and mark the documentation state for each one.
   - Ensure all pages are linked in the nearest `index.md` and higher-level indexes.
   - Ensure the generated site exists in `MicroM/Documentation/Backend/_site`.3. Run `docfx metadata` and `docfx build`.
   - Save logs to `MicroM/docfx-buildlogs/baseline-[namespace].log`.
4. Summarize findings in `iteration-summary.md`:
   - For each task, include **execution result** and **verification result**.
   - Classify issues explicitly:
     - **Warning**: Non-critical issues (e.g., missing XML comments).
     - **Error**: Expected but incomplete (e.g., page not linked in `index.md`).
     - **Failure**: Critical (e.g., docfx build broken).
5. If failures or critical gaps are found, schedule a new iteration to revisit the namespace.
6. Run 1 **Iterative Process (Iterations 1+)**
   - After baseline verification, follow the iterative process to improve documentation.

---

## Iterative Process (Iterations 1+)

After baseline verification, documentation improves in cycles.

### Plan Phase
- Review `iteration-summary.md` and `docs-state-backend.md`.
- Add a **plan block** at the top of `iteration-summary.md`:
  - Define tasks (e.g., “Complete XML docs for Configuration namespace”, “Document Configuration namespace overview”).
  - Reference related files and folders.

### Execution Phase
- Add or update documentation under `MicroM/Documentation/Backend/`.
- Ensure every new page is linked in the nearest `index.md` and higher-level indexes.
- Ensure public APIs have XML comments.
- Record notes in `iteration-notes.md` if details exceed summary scope.

### Verification Phase
- Run `docfx metadata` and `docfx build`.
- Save logs to `MicroM/docfx-buildlogs/iterationN.log`.
- Check for existence of generated site in `MicroM/Documentation/Backend/_site` for pages and links.
- Ensure is `MicroM/Documentation/Backend/_site` and its contents are committed to git.
- For each task:
  - Record execution result.
  - Record verification result with explicit success/failure.
  - Provide reason if verification fails.
  - Classify outcomes as Warning, Error, or Failure.

### Summary & Forward Tasks
- Append results to `iteration-summary.md`:
  - Task outcomes (execution + verification).
  - Verification outcomes with reasoning.
  - Issues encountered.
  - Next iteration tasks (must, should, future).

### Commit Changes
- Commit updated/created files:
  - `Documentation/Backend/...`
  - `Documentation-Progress/Backend/...`
  - `docfx.json`
  - Build logs
- Use commit messages that mention iteration number and high-level results.

---

## Project Layout Notes

- **Backend**: `/MicroM/core` (.NET 8, C#, SQL)
- **Frontend**: `/MicroM/micromlib` (React, TypeScript, Mantine)
- **SSL**: `/MicroM/SSL` (certificate config)
- **Documentation**: `/MicroM/Documentation` (site root for backend + frontend docs)
- **Backend docs**: `/MicroM/Documentation/Backend` (DocFX input/output + conceptual docs)
- **DocFX tool/config**: `/MicroM/docfx` (separate from published docs)
- **Tests**: `/MicroM/LibraryTest` (backend tests with MSTest)
- **Sample API**: `/MicroM/WebAPI` (temporary, may move later)
