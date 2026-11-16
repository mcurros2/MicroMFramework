create or alter  proc [dbo].aad_update
        @ad_configuration_id Char(20)
        , @application_id Char(20)
        , @ad_domain VarChar(2048)
        , @user_principal_domain VarChar(2048)
        , @ad_container VarChar(2048)
        , @ad_server_ip VarChar(64)
        , @ad_user VarChar(256)
        , @ad_password VarChar(2048)
        , @create_user_on_login Bit
        , @default_user_group_id Char(20)
        , @lu DateTime
        , @webusr VarChar(255)
        as

if (@application_id is null or trim(@application_id) = '') begin select 11, 'The parameter @application_id cannot be null or empty' return end

set @default_user_group_id = NULLIF(@default_user_group_id,'')
if @create_user_on_login is null set @create_user_on_login = 0

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    begin tran

    select  @cu=dt_lu
    from    [dbo].[application_ad_configuration] with (rowlock, holdlock, updlock)
    where   c_ad_configuration_id = @ad_configuration_id

    if @cu is null
    begin
        declare @id bigint
        exec [dbo].num_iGetNewNumber 'aad', @nextnumber = @id out
        select @ad_configuration_id = right('0000000000'+rtrim(@id),10)

        insert  [dbo].[application_ad_configuration]
        values
            (
            @ad_configuration_id
            , @application_id
            , @ad_domain
            , @user_principal_domain
            , @ad_container
            , @ad_server_ip
            , @ad_user
            , @ad_password
            , @create_user_on_login
            , @default_user_group_id
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        commit tran
        select    15, rtrim(@ad_configuration_id)
        return
    end

    if @cu<>@lu or @lu is null 
    begin
        commit tran
        select 4, 'Record changed'
        return
    end

    update  [dbo].[application_ad_configuration]
    set     c_application_id = @application_id
            , vc_ad_domain = @ad_domain
            , vc_user_principal_domain = @user_principal_domain
            , vc_ad_container = @ad_container
            , vc_ad_server_ip = @ad_server_ip
            , vc_ad_user = @ad_user
            , vc_ad_password = @ad_password
            , bt_create_user_on_login = @create_user_on_login
            , c_default_user_group_id = @default_user_group_id
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_ad_configuration_id = @ad_configuration_id

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
