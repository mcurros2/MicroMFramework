# Class: MicroM.DataDictionary.Configuration.MenuDefinition
## Overview
Defines an application menu and the access granted to its related entities.

**Inheritance**
Object -> MenuDefinition

## Constructors
| Constructor | Description |
|:------------|:-------------|
| MenuDefinition(string menuDescription) | Initializes a menu definition with description. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| MenuID | string | Identifier of the menu. |
| MenuDescription | string | Description of the menu. |
| MenuItems | CustomOrderedDictionary&lt;MenuItemDefinition&gt; | Ordered collection of menu items. |

## See Also
- [MenuItemDefinition](../MenuItemDefinition/index.md)
- [UsersGroupDefinition](../UsersGroupDefinition/index.md)
