# Class: MicroM.Data.EntityFilter<TFilterEntity>
## Overview
Represents a typed filter definition for an entity.

### Type Parameters
| Parameter | Description |
|:------------|:-------------|
|TFilterEntity|The entity type the filter applies to.|

**Inheritance**
[EntityFilterBase](../EntityFilterBase/index.md) -> EntityFilter

**Implements**
-

## Example Usage
```csharp
var filter = new EntityFilter<MyEntity>("MyFilter");
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| EntityFilter(string name) | Initializes the filter with a name. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Name | string | Inherited from base. |
| FilterEntityType | Type | Inherited from base. |

## Methods
| Method | Description |
|:------------|:-------------|
| - | - |

## See Also
- [EntityFilterBase](../EntityFilterBase/index.md)
