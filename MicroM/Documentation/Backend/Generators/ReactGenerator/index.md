# React Generator

The React generator transforms backend entities into TypeScript classes and forms that consume the MicroM client library.

## Workflow

1. Each entity builds a `TemplateValues` object describing class names, columns, views, lookups, and procedures.
2. Templates in `Templates.cs` describe the structure of category classes, entity definitions, entities, and forms.
3. Extension methods assemble token values and clean up the output using helpers from the shared `Extensions` namespace.

## Key Extension Methods

- `EntityExtensions.AsTypeScriptEntityDefinition` – builds the `{Entity}Def` class with columns, views, lookups, and procedures.
- `EntityExtensions.AsTypeScriptEntity` – creates the runtime entity wrapper that loads forms and icons.
- `EntityExtensions.AsTypeScriptEntityForm` – generates a default form with field controls.
- `CategoriesExtensions.AsTypeScriptCategories` and `LookupExtensions.AsLookupDefinition` provide category and lookup snippets used by the entity templates.

Together these pieces create a ready‑to‑use React client that matches the backend schema.

