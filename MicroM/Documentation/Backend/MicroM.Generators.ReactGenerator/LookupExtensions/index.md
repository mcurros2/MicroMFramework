# Class: MicroM.Generators.ReactGenerator.LookupExtensions
## Overview
Builds lookup definition sections for TypeScript entities.

**Inheritance**
object -> LookupExtensions

**Implements**
None

## Example Usage
```csharp
string lookup = entity.Def.AsLookupDefinition();
```
## Methods
| Method | Description |
|:------------|:-------------|
| AsDefaultLookupDefinitionContent(this IReadOnlyDictionary<string, EntityForeignKeyBase>, string) | Generates default lookup mappings. |
| AsLookupDefinitionContent(this IReadOnlyDictionary<string, EntityForeignKeyBase>, string) | Creates lookup mappings for foreign keys. |
| AsLookupDefinition(this EntityDefinition, string) | Builds the complete lookup definition block. |

## Remarks
None.

