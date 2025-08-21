# Class: MicroM.DataDictionary.Configuration.StatusDefinition
## Overview
Base class used to define a set of status values for an entity.

**Inheritance**
Object -> StatusDefinition

## Constructors
| Constructor | Description |
|:------------|:-------------|
| StatusDefinition() | Initializes a new empty instance. |
| StatusDefinition(string description) | Initializes with description. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| StatusID | string | Identifier for the status set. |
| Description | string | Description for the status set. |
| Values | Dictionary&lt;string, StatusValuesDefinition&gt; | Collection of defined status values. |

## See Also
- [StatusValuesDefinition](../StatusValuesDefinition/index.md)
