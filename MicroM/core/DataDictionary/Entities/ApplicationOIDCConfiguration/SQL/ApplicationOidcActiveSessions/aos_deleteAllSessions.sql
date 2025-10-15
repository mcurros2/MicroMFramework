create or alter proc aos_deleteAllSessions	as

declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()

begin try
    begin tran

    update	microm_users_devices
	set		vc_refreshtoken = null
			, dt_refresh_expiration = null
			, i_refreshcount = 0
			, dt_lu = @now
			, vc_luuser = @login

	delete	application_oidc_active_sessions

    select 0, 'OK'

    commit tran
end try
begin catch
    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch

