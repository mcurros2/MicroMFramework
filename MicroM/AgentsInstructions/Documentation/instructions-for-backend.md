# MicroMFramework Documentation Instructions

This file defines the iterative documentation workflow for documenting the backend of MicroMFramework using DocFX.

---

## Iterative Documentation Plan

### 1. Establish a Baseline (Iteration 0)

**Inventory current documentation (do not fix yet)**
- Read existing files under `MicroM/Documentation/` and note incomplete sections.
- Record findings in `MicroM/Documentation-Progress/Backend/docs-state-backend.md` with headings for each namespace and tutorial.
- Note missing or incomplete XML comments, but do not fix them yet.

**Set up DocFX infrastructure**
- Create a `MicroM/docfx/docfx.json` file that points to:
  - `MicroM/core/` assemblies for XML comment extraction.
  - `MicroM/Documentation/Backend/` as the conceptual docs folder.
- Run `docfx metadata` to generate API metadata.
- Run `docfx build` and capture output in `MicroM/docfx-buildlogs/iteration0-backend.log`.

**Create baseline summary**
- Add `MicroM/Documentation-Progress/Backend/iteration-summary.md` noting:
  - Generated docs status.
  - Missing namespaces or pages.
  - Tasks to address in future iterations.
- Commit `docfx.json`, `docs-state-backend.md`, `iteration-summary.md`, and build log.

---

### 2. Iterative Process (Iterations 1+)

Each iteration starts with a plan and ends with recorded progress.

#### Plan Phase
- Read `iteration-summary.md` to identify outstanding tasks.
- Create a plan block at the top of `iteration-summary.md` for the new iteration:
  - List intended tasks (e.g., “Complete XML docs for Configuration namespace”, “Document Configuration namespace”, “Complete overview tutorial”).
  - Reference related files/paths.

#### Execution Phase
For each task:
- Add or update documentation files under `MicroM/Documentation/Backend/`.
- Update corresponding `index.md` files to include new pages.
- If code changes introduce/modify public APIs, ensure XML comments exist.
- Keep notes in `MicroM/Documentation-Progress/Backend/iteration-notes.md` for any context that might exceed token limits in future iterations.

#### Verification Phase
- Run `docfx metadata` and `docfx build`.
- Append command outputs (success/failures) to `MicroM/docfx-buildlogs/iterationN.log`.
- Review generated site in `MicroM/Documentation/Backend/_site` (read-only check: confirm new pages exist, links work).

#### Summary & Forward-Looking Tasks
- Append a results block to `iteration-summary.md`:
  - Completed items.
  - Issues encountered.
  - New tasks discovered.
- Clearly label tasks that must be tackled in upcoming iterations.

#### Commit Changes
- Commit updated/created files (`Documentation/...`, `docfx.json`, `docs/*`).
- Commit message should mention iteration number and high-level accomplishments.

---

### 3. Strategies for Handling Context Limits

- Use summary files (`iteration-notes.md`, `iteration-summary.md`) to externalize findings, avoiding reliance on internal context.
- Chunk large documents: When editing extensive docs, work on one namespace or tutorial at a time.
- Use `rg "keyword" -n` to quickly locate relevant content without loading large files entirely into context.
- Keep examples small: Reference code snippets from source files rather than embedding large blocks in-memory.

---

### 4. Verification & Maintenance Guidelines

DocFX commands (run every iteration):
```
docfx metadata
docfx build
```

Index maintenance:
- Every new doc page must be linked from the nearest `index.md` and higher-level indexes (namespace → Backend → root).

Checklist per iteration:
- Review previous summary.
- Plan tasks.
- Execute and document.
- Run docfx commands.
- Update summary with outcomes and new tasks.
- Commit.

---

### 5. Future Iterations – Example Tasks

- Fill documentation gap for `Configuration` namespace.
- Complete “overview tutorial” section with database initialization example.
- Add cross‑namespace usage examples.
- Generate contribution guide (`CONTRIBUTING.md`) with documentation rules.
- Integrate doc build verification into CI.

---

## Project Layout Notes

- **Backend**: `/MicroM/core` (.NET 8, C#, SQL)  
- **Frontend**: `/MicroM/micromlib` (React, TypeScript, Mantine)  
- **SSL**: `/MicroM/SSL` (certificate config)  
- **Documentation**: `/MicroM/Documentation` (site root for backend + frontend docs)  
- **Backend docs**: `/MicroM/Documentation/Backend` (DocFX output + conceptual docs)  
- **DocFX tool/config**: `/MicroM/docfx` (kept separate from published docs)  
- **Tests**: `/MicroM/LibraryTest` (backend tests with MSTest)  
- **Sample API**: `/MicroM/WebAPI` (temporary, will move out later)  

---

## Sample files
- **docs-state-backend-sample.md**: Sample documentation state file for backend namespaces and tutorials. Link: [docs-state-backend-sample.md](docs-state-backend-sample.md)
- **iteration-summary-backend-sample.md**: Sample iteration summary file for backend documentation progress. Link: [iteration-summary-backend-sample.md](iteration-summary-backend-sample.md)

---

## Special Notes

- Backend XML documentation was recently enabled but remains incomplete.  
- DocFX output should always go into `/MicroM/Documentation/Backend/_site`.  
- Ensure `/MicroM/Documentation/index.md` links to backend documentation root (`/Backend/index.md`).  

---
