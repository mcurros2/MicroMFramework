create or alter proc aos_deleteSessionGUID	@session_guid uniqueidentifier as

delete application_oidc_active_sessions where c_oidc_session_guid_id = @session_guid;
