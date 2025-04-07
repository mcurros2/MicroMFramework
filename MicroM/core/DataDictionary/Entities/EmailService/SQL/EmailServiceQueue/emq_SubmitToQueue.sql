create or alter proc emq_SubmitToQueue
	@email_configuration_id char(20),
	@sender_name varchar(255), 
	@sender_email varchar(255),
	@subject varchar(255),
	@message varchar(max), 
	@json_destination_and_tags varchar(max),
	@webusr varchar(255)
	as

if (@subject is null or trim(@subject) = '') throw 51000, 'The parameter @subject cannot be null or empty', 1
if (@message is null or trim(@message) = '') throw 51000, 'The parameter @message cannot be null or empty', 1
if (@email_configuration_id is null or trim(@email_configuration_id) = '') throw 51000, 'The parameter @email_configuration_id cannot be null or empty', 1

if not exists (select 1 from email_service_configuration where c_email_configuration_id=@email_configuration_id)  throw 51000, 'The email configuration does not exist', 1

set @message = replace(@message, char(10), '<br />')

select	*
into	#tmp_destinations
from    openjson(@json_destination_and_tags)
        with (
            reference_id varchar(255),
            destination_email varchar(255),
            destination_name varchar(255),
            tags nvarchar(max) as json
        )

-- Expected format
if exists (
    select  1 
    from    #tmp_destinations
    where   tags is null 
            or destination_email is null 
            or destination_name is null
			or reference_id is null
)
    throw 51000, 'Invalid JSON format. Expected [{reference_id: string, destination_email: string, destination_name: string, tags: [{tag: string, value: string}, ...]}]', 1

begin try

	declare @now datetime=getdate(), @login sysname=original_login(), @email_process_id char(36) = newid()

    begin tran

        declare @id bigint
        exec num_iGetNewNumber 'emq', @nextnumber = @id out

		select	id=right('0000000000'+rtrim(@id+row_number() over(order by a.reference_id)),10)
				, a.reference_id
				, sender_email=@sender_email
				, sender_name=@sender_name
				, destination_email=a.destination_email
				, destination_name=a.destination_name
				, vc_subject=dbo.emq_fReplaceTags(@subject, a.tags)
				, vc_body=dbo.emq_fReplaceTags(@message, a.tags)
				, dt_inserttime=@now
				, dt_lu=@now
				, vc_webinsuser=@webusr
				, vc_webluuser=@webusr
				, vc_insuser=@login
				, vc_luuser=@login
		into	#tmp_queue
		from    #tmp_destinations a

		insert	email_service_queue
		select	a.id
				, @email_configuration_id
				, a.reference_id
				, @email_process_id
				, a.sender_email
				, a.sender_name
				, a.destination_email
				, a.destination_name
				, a.vc_subject
				, a.vc_body
				, null -- last error
				, null -- retry count
				, @now
				, @now
				, @webusr
				, @webusr
				, @login
				, @login
		from	#tmp_queue a

		insert	email_service_queue_status
		select	c_email_queue_id=a.id
				, c_status_id='EMAILSTATUS'
				, c_statusvalue_id='QUEUED'
				, a.dt_inserttime
				, a.dt_lu
				, a.vc_webinsuser
				, a.vc_webluuser
				, a.vc_insuser
				, a.vc_luuser
		from	#tmp_queue a

		update  numbering
        set     bi_lastnumber=
				(
					select	max(convert(bigint,id))
					from	#tmp_queue
				)
				, dt_lu = getdate()
				, vc_luuser = @login
				, vc_webluuser = @webusr
		where   c_object_id='emq'

	commit tran

	select	c_email_queue_id=rtrim(a.id)
			, reference_id=a.reference_id
	from	#tmp_queue a

end try
begin catch
    if @@TRANCOUNT > 0
    begin
        rollback
    end;

    throw;
end catch