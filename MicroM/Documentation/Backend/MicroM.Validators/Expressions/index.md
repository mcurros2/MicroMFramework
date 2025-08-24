# Class: MicroM.Validators.Expressions
## Overview
Provides regular expression validators for common MicroM inputs.

**Inheritance**
object -> Expressions

**Implements**
None

## Example Usage
```csharp
bool isValid = Expressions.ValidSQLServerLogin().IsMatch("domain\\user");
```
## Methods
| Method | Description |
|:------------|:-------------|
| [OnlyDigitNumbersAndUnderscore()](OnlyDigitNumbersAndUnderscore/index.md) | Creates a regex that matches alphanumeric characters and underscores. |
| [ValidSQLServerLogin()](ValidSQLServerLogin/index.md) | Creates a regex that validates SQL Server login names including optional domain. |
| [ValidSQLServerPassword()](ValidSQLServerPassword/index.md) | Creates a regex that validates SQL Server passwords with allowed special characters. |

## Remarks
None.

