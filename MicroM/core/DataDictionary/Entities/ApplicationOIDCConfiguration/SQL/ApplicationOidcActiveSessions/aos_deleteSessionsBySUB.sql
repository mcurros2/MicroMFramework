create or alter proc aos_deleteSessionsBySUB @sub varchar(255) as

begin try
    declare	@now datetime=getdate(), @login sysname = ORIGINAL_LOGIN()
    begin tran

    -- Delete all refresh tokens of every device for the user
    update	d
    set		vc_refreshtoken = null
            , dt_refresh_expiration = null
            , i_refreshcount = 0
            , dt_lu = @now
            , vc_luuser = @login
    from	application_oidc_active_sessions a 
            join microm_users_devices d
            on d.c_user_id = a.c_user_id
    where	a.vc_oidc_sub = @sub

    delete application_oidc_active_sessions where vc_oidc_sub = @sub

    commit tran
select 0, 'OK'
end try
begin catch
    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch