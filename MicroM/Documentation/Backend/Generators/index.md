# Generators

MicroM includes a flexible code generation system for both SQL and React code.  Generators use templates combined with extension methods to transform entity definitions into ready-to-use artifacts.  This section provides an overview of the workflow and links to detailed documentation for individual generators and extension helpers.

## Workflow

1. **Template values** – Each generator fills a `TemplateValuesBase` derived object with tokens such as entity names, columns, or view definitions.
2. **Template replacement** – The raw template strings are stored in `Templates.cs` files.  Tokens are replaced using `TemplateExtensions.ReplaceTemplate`.
3. **Post processing** – Text helpers in `Generators.Extensions.CommonExtensions` clean up the generated code by trimming extra blank lines or formatting identifiers.

## Contents

- [Extensions](./Extensions/index.md) – Helpers shared by generators.
- [SQL Generator](./SQLGenerator/index.md) – Creates database schema, procedures, and related SQL.
- [React Generator](./ReactGenerator/index.md) – Produces React entities, forms, and supporting files.

