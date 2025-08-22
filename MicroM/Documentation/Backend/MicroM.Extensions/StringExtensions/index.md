# Class: MicroM.Extensions.StringExtensions
## Overview
Common string manipulation helpers.

**Inheritance**
object -> StringExtensions

**Implements**
None

## Example Usage
```csharp
var name = value.IfNullOrEmpty("unknown");
```
## Methods
| Method | Description |
|:------------|:-------------|
| Truncate(string value, int maxLength) | Truncates a string to the specified length. |
| Unquote(string value, bool unescape = false) | Removes surrounding quotes from a string. |
| Unquote(IEnumerable<string> value) | Removes surrounding quotes from each string in a sequence. |
| Trim(IEnumerable<string> value) | Trims whitespace from each string in a sequence. |
| IfNullOrEmpty(string value, string null_or_empty_value) | Returns a fallback when the string is null or empty. |
| ThrowIfNullOrEmpty(string value, string? parm_name) | Throws if the string is null or empty. |
| IsNullOrEmpty(string? value) | Checks if a string is null or empty. |

## Remarks
None.

