# Class: MicroM.Extensions.BooleanExtensions
## Overview
String helpers for boolean values.

**Inheritance**
object -> BooleanExtensions

**Implements**
None

## Example Usage
```csharp
bool isEnabled = true;
string label = isEnabled.True("Enabled", "Disabled");
```
## Methods
| Method | Description |
|:------------|:-------------|
| True(bool value, string true_value, string false_value = "") | Returns `true_value` when the source is true, otherwise `false_value`. |
| False(bool value, string false_value, string true_value = "") | Returns `false_value` when the source is false, otherwise `true_value`. |

## Remarks
None.

