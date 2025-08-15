# MicromUsersGroupsMenus

Associates groups with menu items they may access.

## Columns
- `c_user_group_id` (PK, FK to `MicromUsersGroups`)
- `c_menu_id` (PK)
- `c_menu_item_id` (PK)

## Relationships
- `FKGroups` – links to `MicromUsersGroups`.
- `FKMenus` – links to `MicromMenusItems`.

## Typical Usage
Controls which menu items are available to members of a group.
