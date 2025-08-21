# Class: MicroM.Core.CustomOrderedDictionary<T>
## Overview
Ordered dictionary with case-insensitive string keys.

### Type Parameters
| Parameter | Description |
|:------------|:-------------|
| T | Type of values stored in the dictionary. |

**Inheritance**
object -> CustomOrderedDictionary<T>

**Implements**
[IReadonlyOrderedDictionary<T>](../IReadonlyOrderedDictionary/index.md)

## Example Usage
```csharp
var dict = new MicroM.Core.CustomOrderedDictionary<int>();
```
## Constructors
| Constructor | Description |
|:------------|:-------------|
| CustomOrderedDictionary() | Creates a new empty dictionary. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Count | int | Number of elements in the dictionary. |
| this[string key] | T? | Gets the value associated with the specified key. |
| this[int index] | T? | Gets the value at the specified index. |
| Values | IEnumerable<T> | Collection of values. |
| Keys | IEnumerable<string> | Collection of keys. |

## Methods
| Method | Description |
|:------------|:-------------|
| Contains(string key) | Determines whether the dictionary contains the specified key. |
| GetEnumerator() | Returns an enumerator that iterates through the values. |
| Add(string key, T value) | Adds an element with the provided key and value. |
| Remove(string key) | Removes the element with the specified key. |
| RemoveAt(int index) | Removes the element at the specified index. |
| TryGetValue(string key, out T? value) | Attempts to retrieve a value for the specified key. |
| TryAdd(string key, T value) | Adds the specified key and value if the key does not already exist. |
| Clear() | Removes all elements from the dictionary. |

## Remarks
None.

## See Also
-
