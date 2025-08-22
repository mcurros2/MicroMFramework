# Class: MicroM.Extensions.EmbeddedResourcesExtensions
## Overview
Utilities for reading embedded SQL resources from assemblies.

**Inheritance**
object -> EmbeddedResourcesExtensions

**Implements**
None

## Example Usage
```csharp
var scripts = await typeof(MyMarker).GetAllCustomProcs("mneo", CancellationToken.None);
```
## Methods
| Method | Description |
|:------------|:-------------|
| GetAllCustomProcs<T>(T assembly_class, string? mneo, CancellationToken ct) where T : class | Reads all embedded SQL resources for a type. |
| GetAssemblyCustomProcs(Assembly assembly, string? mneo, string? starts_with, CancellationToken ct) | Reads embedded SQL resources from an assembly. |

## Remarks
None.

