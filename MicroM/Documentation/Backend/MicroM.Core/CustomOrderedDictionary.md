# Class: MicroM.Core.CustomOrderedDictionary<T>

## Overview
Case-insensitive ordered dictionary that preserves insertion order and provides lookup by key or index.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| CustomOrderedDictionary() | Creates an empty ordered dictionary. |

## Methods
| Method | Description |
|:------------|:-------------|
| Add(string key, T value) | Adds a key/value pair. |
| Remove(string key) | Removes an entry by key. |
| RemoveAt(int index) | Removes an entry by index. |
| TryGetValue(string key, out T value) | Retrieves a value if the key exists. |
| TryAdd(string key, T value) | Adds a key/value pair if the key does not exist. |
| Clear() | Removes all entries. |
| GetEnumerator() | Returns an enumerator over stored values. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| this[string key] | T | Gets a value by key. |
| this[int index] | T | Gets a value by index. |
| Contains(string key) | bool | Indicates if the key exists. |
| Count | int | Number of entries. |
| Values | IEnumerable<T> | Enumerates dictionary values. |
| Keys | IEnumerable<string> | Enumerates dictionary keys. |

## Remarks
Provides ordered storage before .NET 9 introduced an official `OrderedDictionary`.

## See Also
- [IReadonlyOrderedDictionary](IReadonlyOrderedDictionary.md)
