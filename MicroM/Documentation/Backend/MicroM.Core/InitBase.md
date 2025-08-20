# Class: MicroM.Core.InitBase

## Overview
Abstract base class that provides lazy initialization support through an `IsInitialized` flag and `CheckInit` helper.

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| IsInitialized | bool | Indicates whether the object has been initialized. |

## Methods
| Method | Description |
|:------------|:-------------|
| CheckInit() | Throws `ClassNotInitilizedException` if the object is not initialized. |

## Remarks
Classes that require manual initialization can inherit from this base class to enforce initialization checks.

## See Also
- [ClassNotInitilizedException](ClassNotInitilizedException.md)
- [IInit](IInit.md)
