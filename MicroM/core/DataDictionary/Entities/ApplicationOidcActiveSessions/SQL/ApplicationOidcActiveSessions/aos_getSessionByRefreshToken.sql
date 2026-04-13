create or alter proc [dbo].aos_getSessionByRefreshToken @application_id char(20), @oidc_refreshtoken varchar(2048) as

declare	@now datetime = getdate()

select 	c_application_id=rtrim(a.c_application_id),
		c_user_id=rtrim(a.c_user_id),
		c_device_id=rtrim(a.c_device_id),
		a.vc_username,
		a.vc_oidc_session_id,
		a.vc_oidc_refreshtoken,
		a.dt_refresh_expiration,
		a.vc_oidc_sub
from	[dbo].application_oidc_active_sessions a
where	a.c_application_id = @application_id
		and a.vc_oidc_refreshtoken = @oidc_refreshtoken
