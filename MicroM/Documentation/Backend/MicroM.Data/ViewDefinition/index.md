# Class: MicroM.Data.ViewDefinition
## Overview
Maps a stored procedure that acts as a view with parameters and defaults.

**Inheritance**
object -> ViewDefinition

**Implements**
None

## Example Usage
```csharp
var view = new ViewDefinition("view_proc");
```
## Remarks
None.

## Constructors
| Constructor | Description |
|:------------|:-------------|
| ViewDefinition(params string[] parms) | Creates a view definition with default parameters. |
| ViewDefinition(string? name = "", bool add_default_parms = true, params ViewParm[] parms) | Creates a view definition with custom parameters. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| Parms | Dictionary<string, ViewParm> | Parameters for the view. |
| CompoundKeyGroups | Dictionary<string, List<ViewParm>> | Groups of compound key parameters. |
| BrowsingKeyParm | ViewParm | Parameter used as browsing key. |
| Proc | ProcedureDefinition | Procedure representing the view. |
| Filters | EntityFilterBase? | Optional filter entity. |

## Methods
| Method | Description |
|:------------|:-------------|
| CreateFilters<T>(string name = "") | Creates a filter entity for the view. |

