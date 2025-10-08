create or alter proc aos_update
        @application_id char(20)
        , @username varchar(255)
        , @device_id Char(20)
        , @oidc_session_id varchar(255)
        , @oidc_sub varchar(255)
        , @oidc_refreshtoken varchar(255)
        , @refresh_expiration DateTime
        , @lu DateTime
        , @webusr VarChar(255)
        as

if (@application_id is null or trim(@application_id) = '') begin select 11, 'The parameter @application_id cannot be null or empty' return end
if (@username is null or trim(@username) = '') begin select 11, 'The parameter @username cannot be null or empty' return end
if (@device_id is null or trim(@device_id) = '') begin select 11, 'The parameter @device_id cannot be null or empty' return end
if (@oidc_session_id is null) begin select 11, 'The parameter @oidc_session_id cannot be null' return end

set @oidc_sub = nullif(@oidc_sub, '')
set @oidc_refreshtoken = nullif(@oidc_refreshtoken, '')

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    begin tran

    select  @cu=dt_lu
    from    [application_oidc_active_sessions] with (rowlock, holdlock, updlock)
    where   c_application_id = @application_id
            and vc_username = @username
            and c_device_id = @device_id

    if @cu is null
    begin

        insert  [application_oidc_active_sessions]
        values
            (
            @application_id
            , @username
            , @device_id
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

    update  [application_oidc_active_sessions]
    set     vc_oidc_session_id = @oidc_session_id
            , vc_oidc_refreshtoken = @oidc_refreshtoken
            , dt_refresh_expiration = @refresh_expiration
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_application_id = @application_id
            and vc_username = @username
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