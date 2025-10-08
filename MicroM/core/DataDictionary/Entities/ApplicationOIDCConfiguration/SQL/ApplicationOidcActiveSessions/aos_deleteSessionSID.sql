create or alter proc aos_deleteSessionSID @application_id char(20), @oidc_session_id varchar(255) as

delete	application_oidc_active_sessions 
where	c_application_id = @application_id
		and vc_oidc_session_id = @oidc_session_id
