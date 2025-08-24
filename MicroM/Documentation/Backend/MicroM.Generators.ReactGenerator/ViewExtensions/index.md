# Class: MicroM.Generators.ReactGenerator.ViewExtensions
## Overview
Generates TypeScript view definitions from entity metadata.

**Inheritance**
object -> ViewExtensions

**Implements**
None

## Example Usage
```csharp
string views = entity.Def.Views.AsViewsDefinition();
```
## Methods
| Method | Description |
|:------------|:-------------|
| AsViewsDefinition(this IReadOnlyDictionary<string, ViewDefinition>, string) | Produces view mapping objects for a TypeScript entity. |

## Remarks
None.

