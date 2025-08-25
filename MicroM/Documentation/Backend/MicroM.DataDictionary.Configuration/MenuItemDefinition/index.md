# Class: MicroM.DataDictionary.Configuration.MenuItemDefinition
## Overview
Describes a single item within an application menu.

**Inheritance**
Object -> MenuItemDefinition

## Constructors
| Constructor | Description |
|:------------|:-------------|
| MenuItemDefinition(string menuItemDescription, string? parentMenuItemId = null, List<string>? allowed_routes = null) | Initializes a menu item with description, optional parent and allowed routes. |

## Properties
| Property | Type | Description |
|:------------|:-------------|:-------------|
| MenuID | string | Identifier of the owning menu. |
| MenuItemID | string | Identifier of the menu item. |
| ParentMenuItemID | string? | Identifier of the parent menu item. |
| MenuItemDescription | string | Display description for the item. |
| Children | List&lt;MenuItemDefinition&gt; | Child menu items. |
| Parent | MenuItemDefinition? | Reference to parent menu item. |
| AllowedRoutes | List&lt;string&gt;? | Routes allowed for this item. |
| ItemPath | string | Resolved path within the menu. |

## See Also
- [MenuDefinition](../MenuDefinition/index.md)
