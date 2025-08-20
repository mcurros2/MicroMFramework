# Class: MicroM.Core.ClassNotInitilizedException

## Overview
Exception thrown when an object is used before being initialized.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ClassNotInitilizedException() | Creates the exception with a default message. |
| ClassNotInitilizedException(string message) | Creates the exception with a custom message. |

## Remarks
Used by `InitBase` and related classes to enforce initialization.

## See Also
- [InitBase](InitBase.md)
- [IInit](IInit.md)
