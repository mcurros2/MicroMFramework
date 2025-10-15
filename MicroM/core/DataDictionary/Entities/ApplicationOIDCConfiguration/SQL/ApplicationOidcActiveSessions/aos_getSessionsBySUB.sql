create or alter proc aos_getSessionsBySUB @oidc_sub varchar(255) as

select 	c_application_id=rtrim(a.c_application_id),
		a.vc_username,
		c_device_id=rtrim(a.c_device_id),
		a.vc_oidc_session_id,
		a.vc_oidc_refreshtoken,
		a.dt_refresh_expiration
from	application_oidc_active_sessions a
where	a.vc_oidc_sub = @oidc_sub