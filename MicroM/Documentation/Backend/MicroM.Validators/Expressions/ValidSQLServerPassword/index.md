# MicroM.Validators.Expressions.ValidSQLServerPassword
Creates a regex that validates SQL Server passwords with allowed special characters.

### Syntax
```csharp
public static partial Regex ValidSQLServerPassword();
```

## Example Usage
```csharp
bool isValid = Expressions.ValidSQLServerPassword().IsMatch("P@ssw0rd!");
```
## Remarks
None.

## See Also
- [Expressions](../index.md)
