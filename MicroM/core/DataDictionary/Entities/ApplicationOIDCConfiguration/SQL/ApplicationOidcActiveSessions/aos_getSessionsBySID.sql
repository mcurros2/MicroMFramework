create or alter proc aos_getSessionsBySID @application_id char(20), @oidc_session_id varchar(255) as

declare @username varchar(255)

select  @username = a.vc_username
from	aos_oidc_active_sessions a
where	a.c_application_id = @application_id
		and a.vc_oidc_session_id = @oidc_session_id

select 	c_application_id=rtrim(a.c_application_id),
		a.vc_username,
		c_device_id=rtrim(a.c_device_id),
		a.vc_oidc_session_id,
		a.vc_oidc_refreshtoken,
		a.dt_refresh_expiration
from	aos_oidc_active_sessions a
where	a.vc_username = @username
