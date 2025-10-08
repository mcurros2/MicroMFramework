create or alter proc aos_deleteUserSessions	@username VarChar(255) as

delete	application_oidc_active_sessions
where	vc_username = @username
