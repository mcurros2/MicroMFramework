# Interface: MicroM.Core.IInit

## Overview
Interface for classes that support explicit initialization.

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| IsInitialized | bool | Indicates whether the object has been initialized. |

## Methods
| Method | Description |
|:------------|:-------------|
| Init() | Performs initialization logic for the class. |
| CheckInit() | Throws `ClassNotInitilizedException` if the object is not initialized. |

## Remarks
Used to implement lazy initialization patterns across the framework.

## See Also
- [InitBase](InitBase.md)
