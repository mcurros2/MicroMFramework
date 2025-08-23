# MicroMFramework Backend Documentation Instructions

This file defines the iterative documentation workflow for documenting the backend of MicroMFramework. 

**This file contains the instructions to be followed by agents. Templates and samples are provided to be used to create the documentation files.**

---

## Documentation Files & Locations

- **Instructions (this file & samples):** `AgentsInstructions/Documentation/`
  - Instructions for agents live here.
  
  - **Templates in the section are READN ONLY. Agents must not update them.**
    - Sample files (templates only, do not edit):
      - [`docs-state-backend-sample.md`](docs-state-backend-sample.md) → Example and Template for tracking documentation state.
      - [`iteration-summary-backend-sample.md`](iteration-summary-backend-sample.md) → Example and Template for iteration summaries.
    - Documentation templates
      - [`Templates/namespace-documentation-template.md`](Templates/namespace-documentation-template.md) → Template for documenting namespaces.
      - [`Templates/class-doc-template.md`](Templates/class-doc-template.md) → Template for documenting classes.
      - [`Templates/constructor-doc-template.md`](Templates/constructor-doc-template.md) → Template for documenting constructors.
      - [`Templates/method-doc-template.md`](Templates/method-doc-template.md) → Template for documenting methods.
      - [`Templates/enum-doc-template.md`](Templates/enum-doc-template.md) → Template for documenting enums.
      - [`Templates/interface-doc-template.md`](Templates/interface-doc-template.md) → Template for documenting interfaces.
      - [`Templates/struct-doc-template.md`](Templates/struct-doc-template.md) → Template for documenting structs.

- **Progress tracking (results created by AI agents):** `MicroM/Documentation-Progress/Backend/`
  - `docs-state-backend.md` → Maintained record of documentation coverage (namespaces & tutorials).
  - `iteration-summary.md` → Iteration progress and results log.
  - `iteration-notes.md` → Additional notes, context overflow, or long details.

- **Backend documentation site (source & output):** `MicroM/Documentation/Backend/`
  - Must contain an `index.md` at root.
  - Each namespace must have its own folder with its own `index.md`.
  - Sub-folders for each class, struct, enum, and interface

---
## Mandatory rules for documentation
- All documentation must be written in **English**.
- Never modify any C# source code files in the documentation process.
  - Do not add, remove or change any executable code.
  - Do not ad dependencies to the code.
  - Only add or update XML comments in the source code.
- Respect file encoding:
  - Use **UTF-8** encoding for all markdown files.
  - Ensure BOM (Byte Order Mark) is present in the files.
- Respect end of line (EOL) conventions:
  - Use **CRLF** (Line Feed) for line endings in all markdown files.
  - Use **CRLF** for C# source code files.
  
## How to document a namespace
- Each namespace must have its own folder in `MicroM/Documentation/Backend/`.
- Each namespace folder must have an `index.md` file.
- Each namespace `index.md` must follow the [`namespace-documentation-template.md`](Templates/namespace-documentation-template.md) template.
- Each namespace must be listed in `MicroM/Documentation/Backend/index.md`.

## How to document a class, struct, enum, or interface
- Each class, struct, enum, or interface must have its own folder in `MicroM/Documentation/Backend/{{namespace}}`.
- Each class, struct, enum, or interface folder must have an `index.md` file.
- Each class, struct, enum, or interface `index.md` must follow the corresponding template (e.g., [`class-doc-template.md`](Templates/class-doc-template.md)).
- Each class, struct, enum, or interface must be listed in its namespace `index.md`.

---

## Documentation State & Iteration Summary Guidelines

Both `docs-state-backend.md` and `iteration-summary.md` follow the same principles:

- **docs-state-backend.md**
  - Organized by namespace.
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


---

## Baseline Phase (Verification Pass)

The **baseline phase** establishes the current state of documentation for a namespace or for the backend overall.

- Run this phase:
  - At the very start (Iteration 0).
  - When documentation instructions have been updated.
  - On user demand
  - THIS PHASE IS INTENDED FOR DETERMINING THE DOCUMENTATION STATE AND BASE LINE. 
  - DO NOT MODIFY ANY *.CS* FILES IN THIS PHASE.
  - DOC-STATE-BACKEND.MD MUST BE UPDATED TO REFLECT THE CURRENT STATE OF DOCUMENTATION AS A REFERENCE FOR NEXT TASKS IN THE ITERATIVE PROCESS.

### Steps
1. **Delete** `docs-state-backend.md`, `iteration-summary.md`, and `iteration-notes.md`.
   - Start fresh to ensure accurate baseline.
2. Re-scan all namespaces in `MicroM/core` to sync documentation state.
   - Create a new `docs-state-backend.md`.
     - Use [`docs-state-backend-sample.md`](docs-state-backend-sample.md) as a template.
   - Every namespace under MicroM should be listed in `docs-state-backend.md`.
     - namespaces should be listed in alphabetical order.
     - Each namespace should use `namespace-documentation-template.md` as a template for its `index.md`.
   - Create a separate task to Visit each namespace to check:
     - If it is listed and linked in `index.md` in `MicroM/Documentation/Backend/`.
     - If it has an `index.md` in its folder, respecting the `namespace-documentation-template.md` template.
     - If all public APIs have XML comments.
     - If all pages are linked in the nearest `index.md` and higher-level indexes.
     - If all classes, structs, enums, and interfaces are documented, respecting the corresponding templates.
       - If not documented, add a task to document them.
     - Update existing namespace status in `docs-state-backend.md` to reflect current state.
       - A class is considered documented if it has:
         - A folder with the class name.
         - An `index.md` in its folder.
         - All public APIs have XML comments.
         - The page is linked to its namespace `index.md` and higher-level indexes.
         - The folder contains all documentation files.
       - The state should contain:
         - **Complete**: All classes documented, XML comments present, pages linked.
         - **Incomplete**: Some classes not documented, XML comments missing, pages not linked.
         - **Not Started**: No documentation started for the namespace.
     - Add new namespaces to `docs-state-backend.md` with initial status of incomplete.
     - Delete any namespaces that are no longer present in `MicroM/core` from `docs-state-backend.md`.
3. Summarize findings in `iteration-summary.md`:
   - For each task, include **execution result** and **verification result**.
   - Classify issues explicitly:
     - **Warning**: Non-critical issues (e.g., missing XML comments).
     - **Error**: Expected but incomplete (e.g., page not linked in `index.md`).
     - **Failure**: Critical (e.g., docfx build broken).
4. If failures or critical gaps are found, schedule a new iteration to revisit the namespace.
5. In a separate task, run 1 **Iterative Process (Iterations 1+)**
   - After baseline verification, RUN 100 ITERATIONS of the iterative process to continue with the next tasks.

---

## Iterative Process (Iterations 1+, run 100 iterations)

After baseline verification, documentation improves in cycles.

### Plan Phase
- Review the documentation templates in `Templates/`.
  - Check if sections were added or removed.
  - If templates were updated, ensure they are used in the next iteration.
    - Add new tasks for creating any new templates.
    - Add tasks for updating existing templates.
- Review `iteration-summary.md` and `docs-state-backend.md`.
- Add a **plan block** at the top of `iteration-summary.md`:
  - Process one namespace at a time.
    - For each namespace do not plan more than 10 tasks.
    - Add the remaining tasks to be planned in the next iteration.
    - Add XML documentation tasks first.
    - Once XML documentation is complete, update the related templates.
      - Example: If a class is documented:
        - Add a task to update the existing docs using `class-doc-template.md` to reflect the current state.
        - Add a task to update the existing docs using `namespace-documentation-template.md` to reflect the current state.
  - Define tasks
    - “Complete XML docs for the selected namespace”
    - When all XML docs are complete, “Document the selected namespace”
    - Add other documentation tasks as needed.
  - Reference related files and folders.

## Documentations Tasks
- **XML Documentation Tasks**:
- For each public API in the namespace:
  - Add a task to ensure it has XML comments.
  - If missing, add a task to document the source code with XML comments.
- **Markdown Documentation Tasks**:
  - Use these templates for documentation:
    - [`Templates/namespace-documentation-template.md`](Templates/namespace-documentation-template.md) → Template for documenting namespaces.
    - [`Templates/class-doc-template.md`](Templates/class-doc-template.md) → Template for documenting classes.
    - [`Templates/constructor-doc-template.md`](Templates/constructor-doc-template.md) → Template for documenting constructors.
    - [`Templates/method-doc-template.md`](Templates/method-doc-template.md) → Template for documenting methods.
    - [`Templates/enum-doc-template.md`](Templates/enum-doc-template.md) → Template for documenting enums.
    - [`Templates/interface-doc-template.md`](Templates/interface-doc-template.md) → Template for documenting interfaces.
    - [`Templates/struct-doc-template.md`](Templates/struct-doc-template.md) → Template for documenting structs.
  - IMPORTANT: Fill only each section of the template if existing data is available.
  - IMPORTANT: For inheritance only add the base class and implemented interfaces.
  - Follow any instructions in the template.

### Execution Phase
- Add or update documentation under `MicroM/Documentation/Backend/`.
- Create or update the documentation files using the templates in `Templates/`.
- Follow "How to document a namespace" and "How to document a class, struct, enum, or interface" sections."
- Ensure every new page is linked to its namespace `index.md` and higher-level indexes.
- Ensure public APIs have XML comments.
- Record notes in `iteration-notes.md` if details exceed summary scope.
- Record execution result.

### Verification Phase
- For each task:
  - Verify the following:
  - For each markdown file modified or created:
    - Ensure it follows the designated template.
    - Ensure the sections of the template are present if contain data.
    - Ensure cross references are updated.
    - Ensure all classes, structs, enums, and interfaces are documented.
  - Record verification result with explicit success/failure.
  - Provide reason if verification fails.
  - Classify outcomes as Warning, Error, or Failure.

### Summary & Next Tasks
- Append results to `iteration-summary.md`:
  - Task outcomes (execution + verification).
  - Verification outcomes with reasoning.
  - Issues encountered.
  - Next iteration tasks:
    - Add tasks to document detected missing XML comments.
    - Add tasks to update referenced namespaces, classes, etc.
  - If the documentation is complete and no further tasks are needed, mark the namespace as **Complete** in `docs-state-backend.md`.
  - If all namespaces are complete, mark the backend documentation as **Complete** in `docs-state-backend.md` and stop the iterative process.

### Commit Changes
- Commit updated/created files:
  - `Documentation/Backend/...`
  - `Documentation-Progress/Backend/...`
  - XML documentation changes.
- Use commit messages that mention iteration number and high-level results.

---

## Project Layout Notes

- **Backend**: `/MicroM/core` (.NET 8, C#, SQL)
- **Frontend**: `/MicroM/micromlib` (React, TypeScript, Mantine)
- **SSL**: `/MicroM/SSL` (certificate config)
- **Documentation**: `/MicroM/Documentation` (site root for backend + frontend docs)
- **Backend docs**: `/MicroM/Documentation/Backend` (Documentation md files)
- **Tests**: `/MicroM/LibraryTest` (backend tests with MSTest)
- **Sample API**: `/MicroM/WebAPI` (temporary, may move later)
