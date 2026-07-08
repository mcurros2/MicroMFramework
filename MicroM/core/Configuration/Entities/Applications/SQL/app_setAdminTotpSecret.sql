create or alter proc [dbo].app_setAdminTotpSecret
    @application_id char(20)
    , @app_admin_totp_secret varchar(2048)
as

if @application_id is null or @application_id=''
begin
    select 11, 'Application ID is null or empty'
    return
end

if @app_admin_totp_secret is null or @app_admin_totp_secret=''
begin
    select 11, 'SQL admin TOTP secret is null or empty'
    return
end

declare @now datetime=getdate(), @login sysname = original_login()

begin try
    begin tran

    update  [dbo].[applications]
    set     vc_app_admin_totp_secret = @app_admin_totp_secret
            , dt_lu = @now
            , vc_luuser = @login
    where   c_application_id = @application_id

    if @@rowcount = 0
    begin
        rollback
        select 11, 'Application not found'
        return
    end

    commit tran
    select 0, 'OK'

end try
begin catch
    if @@trancount > 0
    begin
        rollback
    end;

    throw;
end catch
