# Class: MicroM.Core.InitBase
## Overview
Provides lazy initialization support for derived classes.

**Inheritance**
object -> InitBase

**Implements**
None

## Example Usage
```csharp
class MyClass : InitBase {
    public void Init() { IsInitialized = true; }
}
```
## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| IsInitialized | bool | Indicates whether initialization occurred. |

## Methods
| Method | Description |
|:------------|:-------------|
| CheckInit() | Throws if the class has not been initialized. |

## Remarks
None.

## See Also
- [EntityBase](../EntityBase/index.md)
