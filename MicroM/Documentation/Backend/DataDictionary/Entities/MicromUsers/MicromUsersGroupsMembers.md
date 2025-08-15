# MicromUsersGroupsMembers

Many‑to‑many association between users and groups.

## Columns
- `c_user_group_id` (PK, FK to `MicromUsersGroups`)
- `c_user_id` (PK, FK to `MicromUsers`)

## Relationships
- `FKMicromUsers` – links to `MicromUsers`.
- `FKGroups` – links to `MicromUsersGroups`.

## Typical Usage
Defines membership of users inside groups.
