# Interface: MicroM.Core.IReadonlyOrderedDictionary<T>
## Overview
Represents a read-only ordered dictionary.

## Properties
| Property | Description |
|:------------|:-------------|
| Count | Gets the number of elements contained in the dictionary. |
| this[string key] | Gets the element associated with the specified key. |
| this[int index] | Gets the element at the specified index. |
| Values | Gets the collection of values. |
| Keys | Gets the collection of keys. |

## Methods
| Method | Description |
|:------------|:-------------|
| Contains(string key) | Determines whether the dictionary contains the specified key. |
| GetEnumerator() | Returns an enumerator that iterates through the values. |
| TryGetValue(string key, out T? value) | Gets the value associated with the specified key. |

## Remarks
None.

## See Also
-
