# Class: MicroM.Core.EntityActionBase
## Overview
Base class for custom entity actions.

**Inheritance**
object -> EntityActionBase

**Implements**
None

## Example Usage
```csharp
class MyAction : EntityActionBase {
    public override Task<EntityActionResult> Execute(EntityBase e, DataWebAPIRequest p, EntityDefinition d, MicroMOptions? o, IWebAPIServices? a, IMicroMEncryption? enc, CancellationToken ct, string? app) {
        return Task.FromResult<EntityActionResult>(new EmptyActionResult());
    }
}
```
## Methods
| Method | Description |
|:------------|:-------------|
| Execute(EntityBase entity, DataWebAPIRequest parms, EntityDefinition def, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id) | Executes the action. |

## Remarks
None.

## See Also
-
