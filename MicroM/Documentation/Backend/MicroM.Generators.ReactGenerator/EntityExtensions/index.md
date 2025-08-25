# Class: MicroM.Generators.ReactGenerator.EntityExtensions
## Overview
Creates TypeScript entity definitions, classes, and forms.

**Inheritance**
object -> EntityExtensions

**Implements**
None

## Example Usage
```csharp
string def = entity.AsTypeScriptEntityDefinition();
```
## Methods
| Method | Description |
|:------------|:-------------|
| AsTypeScriptEntityDefinition<T>(this T) where T : EntityBase | Builds a TypeScript entity definition file. |
| AsTypeScriptEntity<T>(this T) where T : EntityBase | Generates a TypeScript entity class. |
| AsTypeScriptEntityForm<T>(this T) where T : EntityBase | Creates a TypeScript form component for the entity. |

## Remarks
None.

