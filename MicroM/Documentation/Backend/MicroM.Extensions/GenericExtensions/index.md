# Class: MicroM.Extensions.GenericExtensions
## Overview
Generic utility methods for comparisons and conversions.

**Inheritance**
object -> GenericExtensions

**Implements**
None

## Example Usage
```csharp
if (value.IsIn(1,2,3)) {
    // do something
}
```
## Methods
| Method | Description |
|:------------|:-------------|
| IsIn<T>(T value, T[] parms, IEqualityComparer<T>? comparer = null) | Checks whether a value exists in an array using an optional comparer. |
| AreAllEqual<T>(T value, T[] parms, bool ignore_null = false, IEqualityComparer<T>? comparer = null) | Determines if all provided parameters equal the value. |
| IsIn<T>(T value, params T[] parms) | Checks whether a value exists in a parameter list. |
| HasAnyFlag<T>(T value, T flags) where T : Enum | Checks if an enum has any of the specified flags. |
| HasAllFlags<T>(T value, T flags) where T : Enum | Checks if an enum has all of the specified flags. |
| TryConvertFromString<T>(string source, out T? result) where T : struct | Attempts to convert a string into a struct type. |
| IsNullOrEmpty<T>(T? collection) where T : ICollection<T> | Determines if a collection is null or empty. |
| IsNullOrEmpty<T>(IList<T>? collection) | Determines if a list is null or empty. |

## Remarks
None.

## See Also
-
