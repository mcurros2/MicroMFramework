# Class: MicroM.DataDictionary.NumberingDef
## Overview
Schema definition for sequential number generation.

**Inheritance**
EntityDefinition -> NumberingDef

## Constructors
| Constructor | Description |
|:------------|:-------------|
| NumberingDef() | Initializes a new instance. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| c_object_id | Column<string> | Identifier of the object being numbered. |
| bi_lastnumber | Column<long> | Last number generated for the object. |
| num_brwStandard | ViewDefinition | Default browse view keyed by object ID. |
| FKObjects | EntityForeignKey<Objects, Numbering> | Reference to the related object. |

## See Also
- [Numbering](../Numbering/index.md)
