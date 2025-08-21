# Class: MicroM.DataDictionary.Configuration.CategoryDefinition
## Overview
Base class used to describe a category and its possible values.

**Inheritance**
Object -> CategoryDefinition

## Constructors
| Constructor | Description |
|:------------|:-------------|
| CategoryDefinition() | Initializes a new empty instance. |
| CategoryDefinition(string description, bool multivalue = false) | Initializes with description and multivalue option. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| CategoryID | string | Unique identifier of the category. |
| Description | string | Human readable description. |
| Multivalue | bool | Indicates if multiple values are allowed. |
| Values | Dictionary<string, CategoryValuesDefinition> | Collection of defined category values. |

## See Also
- [CategoryValuesDefinition](../CategoryValuesDefinition/index.md)
