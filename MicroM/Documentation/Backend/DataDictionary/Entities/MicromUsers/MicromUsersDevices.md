# MicromUsersDevices

Tracks devices used by users for authentication.

## Columns
- `c_user_id` (PK, FK to `MicromUsers`)
- `c_device_id` (PK)
- `vc_useragent`
- `vc_ipaddress`
- `vc_refreshtoken`
- `dt_refresh_expiration`
- `i_refreshcount`

## Relationships
- `FKMicromUsers` – links to `MicromUsers`.

## Typical Usage
Stores refresh tokens and metadata per device. Includes procedure `usd_refreshToken` to rotate tokens.
