# Class: MicroM.DataDictionary.Configuration.StatusValuesDefinition
## Overview
Represents an allowed value within a status definition.

**Inheritance**
Object -> StatusValuesDefinition

## Constructors
| Constructor | Description |
|:------------|:-------------|
| StatusValuesDefinition() | Initializes a new empty instance. |
| StatusValuesDefinition(string description, bool initialValue = false, string value_id = "") | Initializes with description, initial flag and optional identifier. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| StatusValueID | string | Identifier for the status value. |
| Description | string | Description for the status value. |
| InitialValue | bool | Indicates if this is the initial status. |

## See Also
- [StatusDefinition](../StatusDefinition/index.md)
