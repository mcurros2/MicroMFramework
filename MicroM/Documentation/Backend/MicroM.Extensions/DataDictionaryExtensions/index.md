# Class: MicroM.Extensions.DataDictionaryExtensions
## Overview
Helpers for adding definitions and instances to the data dictionary.

**Inheritance**
object -> DataDictionaryExtensions

**Implements**
None

## Example Usage
```csharp
await menuDefinition.AddMenu(entityClient, CancellationToken.None);
```
## Methods
| Method | Description |
|:------------|:-------------|
| AddStatusRelations(EntityBase entity, IEntityClient ec, CancellationToken ct) | Adds related status records for an entity. |
| AddCategoryRelations(EntityBase entity, IEntityClient ec, CancellationToken ct) | Adds related category records for an entity. |
| AddCategoryValue(Categories cat, IEntityClient ec, string categoryvalue_id, string description, CancellationToken ct) | Adds a new value to an existing category. |
| AddStatusValue(Status stat, IEntityClient ec, string statusvalue_id, string description, bool init_value, CancellationToken ct) | Adds a new value to an existing status definition. |
| AddStatus(StatusDefinition stc, IEntityClient ec, CancellationToken ct) | Creates a status record and its initial value. |
| AddCategory(CategoryDefinition cac, IEntityClient ec, CancellationToken ct) | Creates a category record and its values. |
| AddToDataDictionary<T>(T ent, CancellationToken ct) where T : EntityBase, new() | Adds an entity definition to the data dictionary. |
| AddInstanceToDataDictionary(EntityBase ent, CancellationToken ct) | Persists an entity instance in the data dictionary. |
| AddMenu(MenuDefinition menu_definition, IEntityClient ec, CancellationToken ct) | Adds a menu definition using the entity client. |
| AddUserGroup(UsersGroupDefinition user_group, IEntityClient ec, CancellationToken ct) | Adds a user group definition using the entity client. |

## Remarks
None.

