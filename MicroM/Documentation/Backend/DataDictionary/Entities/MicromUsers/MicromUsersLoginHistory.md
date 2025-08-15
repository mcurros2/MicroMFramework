# MicromUsersLoginHistory

Audit table storing login attempts.

## Columns
- `c_user_history_id` (PK)
- `c_user_id` (PK, FK to `MicromUsers`)
- `vc_useragent`
- `vc_ipaddress`
- `bt_success`
- `dt_login_attempt`

## Typical Usage
Used to record successful or failed login attempts for monitoring and security analysis.
