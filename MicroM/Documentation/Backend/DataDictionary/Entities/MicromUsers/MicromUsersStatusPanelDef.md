# MicromUsersStatusPanelDef

Reusable definition that extends other entities with user‑creation fields.

## Columns
- `vc_username`
- `vc_password`
- `vc_user_groups` – array of groups
- `bt_disabled`
- `bt_islocked`
- `i_badlogonattempts`
- `i_locked_minutes_remaining`
- `dt_locked`
- `dt_last_login`
- `dt_last_refresh`

## Typical Usage
Embed this definition into other entities that need to create or update users, such as administrative screens.
