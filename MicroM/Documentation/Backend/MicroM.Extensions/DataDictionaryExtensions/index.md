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
| AddToDataDictionary<T>(T ent, CancellationToken ct) where T : EntityBase, new() | Adds an entity definition to the data dictionary. |
| AddInstanceToDataDictionary(EntityBase ent, CancellationToken ct) | Persists an entity instance in the data dictionary. |
| AddMenu(MenuDefinition menu_definition, IEntityClient ec, CancellationToken ct) | Adds a menu definition using the entity client. |
| AddUserGroup(UsersGroupDefinition user_group, IEntityClient ec, CancellationToken ct) | Adds a user group definition using the entity client. |

## Remarks
None.

## See Also
-
