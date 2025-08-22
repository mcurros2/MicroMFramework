# Class: MicroM.Generators.ReactGenerator.CategoriesExtensions
## Overview
Generates TypeScript code for category relationships.

**Inheritance**
object -> CategoriesExtensions

**Implements**
None

## Example Usage
```csharp
string lookup = columns.AsLookupDefinitionContentCategories();
```
## Methods
| Method | Description |
|:------------|:-------------|
| AsLookupDefinitionContentCategories(this IReadonlyOrderedDictionary<ColumnBase>, string) | Builds lookup definition block for related categories. |
| AsEmbeddedCategoriesImport(this IReadonlyOrderedDictionary<ColumnBase>) | Lists category entities to import. |
| AsCategoriesEntities(this IReadonlyOrderedDictionary<ColumnBase>, Dictionary<string, Type>) | Generates TypeScript classes for related categories. |

## Remarks
None.

## See Also
-
