create or alter proc [dbo].aos_update
        @application_id char(20)
        , @device_id Char(20)
        , @user_id Char(20)
        , @username varchar(255)
        , @oidc_session_id varchar(255)
        , @oidc_sub varchar(255)
        , @oidc_refreshtoken varchar(255)
        , @refresh_expiration DateTime
        , @lu DateTime
        , @webusr VarChar(255)
        as

if (@application_id is null or trim(@application_id) = '') begin select 11, 'The parameter @application_id cannot be null or empty' return end
if (@user_id is null or trim(@user_id) = '') begin select 11, 'The parameter @user_id cannot be null or empty' return end
if (@device_id is null or trim(@device_id) = '') begin select 11, 'The parameter @device_id cannot be null or empty' return end

if (@oidc_session_id is null) begin select 11, 'The parameter @oidc_session_id cannot be null' return end

set @oidc_sub = nullif(@oidc_sub, '')
set @oidc_refreshtoken = nullif(@oidc_refreshtoken, '')
set @username = nullif(@username, '')

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    if @username is null
    begin
        select  @username = a.vc_username
        from    [dbo].microm_users a
        where   a.c_user_id = @user_id

        if @username is null
        begin
            select 11, 'The parameter @username cannot be determined for the provided @user_id'
            return
        end
    end

    begin tran

    select  @cu=dt_lu
    from    [dbo].[application_oidc_active_sessions] with (rowlock, holdlock, updlock)
    where   c_application_id = @application_id
            and c_user_id = @user_id
            and c_device_id = @device_id

    if @cu is null
    begin

        insert  [dbo].[application_oidc_active_sessions]
        values
            (
            @application_id
            , @user_id
            , @device_id
            , @username
            , @oidc_session_id
            , @oidc_sub
            , @oidc_refreshtoken
            , @refresh_expiration
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        commit tran
        select   0, 'OK'
        return
    end

    update  [dbo].[application_oidc_active_sessions]
    set     vc_oidc_session_id = @oidc_session_id
            , vc_oidc_refreshtoken = @oidc_refreshtoken
            , dt_refresh_expiration = @refresh_expiration
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id
            and c_user_id = @user_id
            and c_device_id = @device_id

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