# MicromUsersGroups

Defines security groups for users.

## Columns
- `c_user_group_id` (PK)
- `vc_user_group_name`
- `vc_group_members` – list of member identifiers (fake)

## Relationships
- `UNGroupName` unique constraint on `vc_user_group_name`.
- `FKUsers` (fake) links to [MicromUsers](MicromUsers.md).

## Typical Usage
Groups are used to assign permissions and menu access to multiple users.
