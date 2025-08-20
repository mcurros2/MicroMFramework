# Interface: MicroM.Core.IReadonlyOrderedDictionary<T>

## Overview
Abstraction for a read-only ordered dictionary with lookup by key or index.

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| this[string key] | T | Retrieves the value associated with the specified key. |
| this[int index] | T | Retrieves the value at the specified index. |
| Count | int | Number of items in the dictionary. |
| Values | IEnumerable<T> | Enumerates stored values. |
| Keys | IEnumerable<string> | Enumerates stored keys. |

## Methods
| Method | Description |
|:------------|:-------------|
| Contains(string key) | Determines whether the dictionary contains a key. |
| GetEnumerator() | Returns an enumerator over the values. |
| TryGetValue(string key, out T value) | Attempts to retrieve the value for a key. |

## Remarks
Implemented by `CustomOrderedDictionary` to provide ordered access with optional mutability.

## See Also
- [CustomOrderedDictionary](CustomOrderedDictionary.md)
