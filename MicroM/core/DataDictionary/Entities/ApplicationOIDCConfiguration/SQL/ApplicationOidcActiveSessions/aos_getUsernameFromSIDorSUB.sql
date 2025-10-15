create or alter proc aos_getUsernameFromSIDorSUB @application_id char(20), @oidc_session_id varchar(255), @sub varchar(255) as

select	top 1
		a.vc_username
from	application_oidc_active_sessions a
where	a.c_application_id = @application_id
		and (a.vc_oidc_session_id = @oidc_session_id or a.vc_oidc_sub = @sub)
