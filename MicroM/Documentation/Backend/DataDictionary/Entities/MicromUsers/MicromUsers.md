# MicromUsers

Represents users within MicroM.

## Columns
- `c_user_id` (PK)
- `vc_username`
- `vc_email`
- `vc_pwhash`
- `vb_sid`
- `i_badlogonattempts`
- `bt_disabled`
- `dt_locked`
- `dt_last_login`
- `dt_last_refresh`
- `vc_recovery_code`
- `dt_last_recovery`
- `c_usertype_id` – category [`UserTypes`](../../CategoriesDefinitions/UserTypes.md)
- `vc_user_groups` – list of groups (fake)
- `bt_islocked`, `i_locked_minutes_remaining` – lock status (fake)
- `vc_password` – plain text placeholder (fake)

## Relationships
- `UNUsername` unique constraint on `vc_username`.
- `FKGroups` (fake) links to [MicromUsersGroups](MicromUsersGroups.md).

## Typical Usage
Stores credential and status information for each user. Contains helper procedures for login attempts, password resets and claim retrieval.
