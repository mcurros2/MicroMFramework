create or alter proc [dbo].aos_getSessionsBySUB @oidc_sub varchar(255) as

select 	c_application_id=rtrim(a.c_application_id),
		c_user_id=rtrim(a.c_user_id),
		c_device_id=rtrim(a.c_device_id),
		a.vc_username,
		a.vc_oidc_session_id,
		a.vc_oidc_refreshtoken,
		a.dt_refresh_expiration,
		a.vc_oidc_sub
from	[dbo].application_oidc_active_sessions a
where	a.vc_oidc_sub = @oidc_sub