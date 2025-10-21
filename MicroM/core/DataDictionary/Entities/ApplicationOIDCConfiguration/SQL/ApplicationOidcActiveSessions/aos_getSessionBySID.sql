create or alter proc aos_getSessionBySID @application_id char(20), @oidc_session_id varchar(255) as

select 	c_application_id=rtrim(a.c_application_id),
		c_user_id=rtrim(a.c_user_id),
		c_device_id=rtrim(a.c_device_id),
		a.vc_username,
		a.vc_oidc_session_id,
		a.vc_oidc_refreshtoken,
		a.dt_refresh_expiration,
		a.vc_oidc_sub
from	application_oidc_active_sessions a
where	a.c_application_id = @application_id
		and a.vc_oidc_session_id = @oidc_session_id