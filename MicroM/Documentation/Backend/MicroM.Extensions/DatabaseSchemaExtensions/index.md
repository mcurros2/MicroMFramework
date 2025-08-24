# Class: MicroM.Extensions.DatabaseSchemaExtensions
## Overview
Extensions for creating custom procedures from assembly resources.

**Inheritance**
object -> DatabaseSchemaExtensions

**Implements**
None

## Example Usage
```csharp
await typeof(MyAssemblyMarker).Assembly.CreateAssemblyCustomProcs(entityClient, CancellationToken.None);
```
## Methods
| Method | Description |
|:------------|:-------------|
| CreateAssemblyCustomProcs(Assembly assembly, IEntityClient ec, CancellationToken ct, string? mneo = null, string? starts_with = null) | Builds custom procedures defined as embedded SQL resources. |
| CreateAllCategories(Assembly asm, IEntityClient ec, CancellationToken ct) | Creates all category definitions found in the assembly. |
| CreateAllStatus(Assembly asm, IEntityClient ec, CancellationToken ct) | Creates all status definitions found in the assembly. |

## Remarks
None.

