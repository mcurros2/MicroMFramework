create or alter proc emq_SubmitEmailTemplate
	@email_configuration_id char(20),
	@email_template_id char(20),
	@json_destination_and_tags varchar(max),
    @result int out,
    @msg varchar(max) out,
	@webusr varchar(255)
	as

declare @subject varchar(1024), @message varchar(max), @sender_name varchar(255), @sender_email varchar(255)

select  @subject = a.vc_template_subject
        , @message = a.vc_template_body
from    email_service_templates a
where   a.c_email_template_id = @email_template_id        

if @subject is null
begin
    select  @result = 11, @msg = 'The email template does not exist. TemplateID: '+rtrim(@email_template_id)
    return
end

select  @sender_email = a.vc_default_sender_email
        , @sender_name = a.vc_default_sender_name
from    email_service_configuration a
where   a.c_email_configuration_id = @email_configuration_id

if @sender_email is null
begin
    select  top 1
            @sender_email = a.vc_default_sender_email
            , @sender_name = a.vc_default_sender_name
            , @email_configuration_id = a.c_email_configuration_id
    from    email_service_configuration a
end

if @sender_email is null
begin
    select  @result = 11, @msg = 'The email service is not configured. ConfigID: '+rtrim(@email_configuration_id)
    return
end


begin try

    declare @email_process_id char(36)
    exec emq_SubmitToQueueProcess @email_configuration_id,@sender_name,@sender_email,@subject,@message,@json_destination_and_tags,@webusr,@result out,@email_process_id out

    select  @msg = @email_process_id

end try
begin catch

    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    select @result = 11, @msg = 'An error ocurred while submitting the email to queue: '+error_message()

end catch