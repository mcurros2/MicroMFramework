# MicroM.Validators.Expressions.ValidSQLServerLogin
Creates a regex that validates SQL Server login names including optional domain.

### Syntax
```csharp
public static partial Regex ValidSQLServerLogin();
```

## Example Usage
```csharp
bool isValid = Expressions.ValidSQLServerLogin().IsMatch("domain\\user");
```
## Remarks
None.

## See Also
- [Expressions](../index.md)
