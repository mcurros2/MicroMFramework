create or alter proc emq_update
        @email_queue_id Char(20)
        , @email_configuration_id Char(20)
        , @external_reference varchar(255)
        , @email_process_id Char(36)
        , @sender_email VarChar(2048)
        , @sender_name VarChar(255)
        , @destination_email VarChar(2048)
        , @destination_name VarChar(255)
        , @subject VarChar(255)
        , @message VarChar(max)
        , @last_error VarChar(max)
        , @retries Int
        , @emailstatus_id Char(20)
        , @json_destination_and_tags VarChar(max)
        , @email_template_id char(20)
        , @lu DateTime
        , @webusr VarChar(255)
        as

set @last_error = NULLIF(@last_error,'')

begin try
    declare @cu datetime, @now datetime=getdate(), @login sysname=original_login()

    begin tran

    select  @cu=dt_lu
    from    [email_service_queue] with (rowlock, holdlock, updlock)
    where   c_email_queue_id = @email_queue_id

    if @cu is null
    begin
        if (@email_configuration_id is null or trim(@email_configuration_id) = '') 
        begin 
            rollback
            select 11, 'The parameter @email_configuration_id cannot be null or empty' 
            return 
        end


        declare @id bigint
        exec num_iGetNewNumber 'emq', @nextnumber = @id out
        select @email_queue_id = right('0000000000'+rtrim(@id),10)

        insert  [email_service_queue]
        values
            (
            @email_queue_id
            , @email_configuration_id
            , @external_reference
            , @email_process_id
            , @sender_email
            , @sender_name
            , @destination_email
            , @destination_name
            , @subject
            , @message
            , @last_error
            , @retries
            , @now
            , @now
            , @webusr
            , @webusr
            , @login
            , @login
            )

        insert  [email_service_queue_status]
        select  @email_queue_id
                , a.c_status_id
                , a.c_statusvalue_id
                , @now
                , @now
                , @webusr
                , @webusr
                , @login
                , @login
        from    status_values a
                join objects_status b
                on(b.c_status_id = a.c_status_id)
                join [objects] c
                on(c.c_object_id = b.c_object_id)
        where   c.c_mneo_id = 'emq' and
                a.bt_initial_value = 1

        commit tran
        select    15, rtrim(@email_queue_id)
        return
    end

    if @cu<>@lu or @lu is null 
    begin
        commit tran
        select 4, 'Record changed'
        return
    end

    update  [email_service_queue]
    set     vc_last_error = @last_error
            , i_retries = @retries
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_email_queue_id = @email_queue_id

    update  [email_service_queue_status]
    set     c_statusvalue_id = @emailstatus_id
            , vc_webluuser = @webusr
            , vc_luuser = @login
            , dt_lu = @now
    where   c_email_queue_id = @email_queue_id
            and c_status_id = 'EmailStatus'
            and c_statusvalue_id <> @emailstatus_id

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