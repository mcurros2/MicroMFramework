create or alter proc eqc_iupdate
        @email_configuration_id Char(20)
        , @smtp_host VarChar(2048)
        , @smtp_port Int
        , @user_name VarChar(255)
        , @password VarChar(2048)
        , @use_ssl Bit
        , @default_sender_email VarChar(255)
        , @default_sender_name VarChar(255)
        , @template_subject VarChar(255)
        , @template_body VarChar(max)
        , @lu DateTime
        , @webusr VarChar(255)
        , @result int output
        , @msg varchar(255) output
        as

if @@trancount = 0 throw 50001, 'eqc_iupdate must be called within a transaction', 1

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    declare @email_template_id char(20)='RECOVERY'

    select  @cu=dt_lu
    from    [email_service_configuration] with (rowlock, holdlock, updlock)
    where   c_email_configuration_id = @email_configuration_id

    if @template_subject is not null and @template_body is not null
    and not exists(select 1 from [email_service_templates] where c_email_template_id=@email_template_id)
    begin
        insert  [email_service_templates]
        values
            (
            @email_template_id
            , @template_subject
            , @template_body
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
    end

    if @cu is null
    begin

        insert  [email_service_configuration]
        values
            (
            @email_configuration_id
            , @smtp_host
            , @smtp_port
            , @user_name
            , @password
            , @use_ssl
            , @default_sender_email
            , @default_sender_name
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )
        

        select    @result = 0, @msg = 'OK'
        return
    end

    if @cu<>@lu or @lu is null 
    begin
        select @result = 4, @msg = 'Record changed'
        return
    end

    update  [email_service_configuration]
    set     vc_smtp_host = @smtp_host
            , i_smtp_port = @smtp_port
            , vc_user_name = @user_name
            , vc_password = @password
            , bt_use_ssl = @use_ssl
            , vc_default_sender_email = @default_sender_email
            , vc_default_sender_name = @default_sender_name
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_email_configuration_id = @email_configuration_id

    if @template_subject is not null and @template_body is not null
    begin

        update  [email_service_templates]
        set     vc_template_subject = @template_subject
                , vc_template_body = @template_body
                , vc_webluuser = @webusr
                , vc_luuser = @login
                , dt_lu = @now
        where   c_email_template_id = @email_template_id

    end
    else
    begin

        delete  [email_service_templates]
        where   c_email_template_id = @email_template_id

    end


    select @result = 0, @msg = 'OK'

end try
begin catch

    throw;

end catch
