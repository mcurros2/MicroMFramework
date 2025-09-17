create or alter proc aos_deleteUserSessions	@user_id Char(20) as

delete	application_oidc_server_sessions
where	c_user_id = @user_id
