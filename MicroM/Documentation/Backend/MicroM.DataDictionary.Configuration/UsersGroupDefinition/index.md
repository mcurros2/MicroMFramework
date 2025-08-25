# Class: MicroM.DataDictionary.Configuration.UsersGroupDefinition
## Overview
Defines a user group and the menu items it is allowed to access.

**Inheritance**
Object -> UsersGroupDefinition

## Constructors
| Constructor | Description |
|:------------|:-------------|
| UsersGroupDefinition(string menuDescription) | Initializes a user group with description. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| GroupID | string | Identifier of the group. |
| GroupDescription | string | Description of the group. |
| AllowedMenuItems | Dictionary&lt;string, MenuItemDefinition&gt; | Menu items allowed for the group. |

## Methods
| Method | Description |
|:------------|:-------------|
| AddMenuItem(MenuItemDefinition menuItem) | Adds a single menu item to the allowed list. |
| AddMenuAllItems(MenuDefinition menu) | Adds all menu items from a menu definition. |

## See Also
- [MenuDefinition](../MenuDefinition/index.md)
