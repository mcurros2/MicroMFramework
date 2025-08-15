# Generator Extensions

Common helper methods used across generators live under `MicroM.core.Generators.Extensions`.

## Text Utilities

- `RemoveEmptyLines` – Collapses multiple blank lines to a single line using `GeneratorsRegex.MultipleEmptyLines`.
- `AddSpacesAndLowercaseShortWords` – Splits camel‑case identifiers into words and lowercases short words for friendlier titles.

## Template Helpers

- `ReplaceTemplate` – Replaces tokens in a template using values from a `TemplateValuesBase` instance.

These methods keep generated files readable and free of repetitive boilerplate.

