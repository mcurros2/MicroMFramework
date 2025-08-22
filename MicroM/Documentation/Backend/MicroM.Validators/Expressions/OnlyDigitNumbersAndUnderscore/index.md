# MicroM.Validators.Expressions.OnlyDigitNumbersAndUnderscore
Creates a regex that matches strings containing only digits, letters, and underscores.

### Syntax
```csharp
public static partial Regex OnlyDigitNumbersAndUnderscore();
```

## Example Usage
```csharp
bool isValid = Expressions.OnlyDigitNumbersAndUnderscore().IsMatch("value_123");
```
## Remarks
None.

## See Also
- [Expressions](../index.md)
