create or alter proc emq_qryGetQueuedItems as

select	c_email_configuration_id = rtrim(a.c_email_configuration_id)
		, c_email_queue_id=rtrim(a.c_email_queue_id)
		, c_emailstatus_id = rtrim(b.c_statusvalue_id)
		, a.vc_destination_email
		, a.vc_destination_name
		, a.vc_sender_email
		, a.vc_sender_name
		, a.vc_subject
		, a.vc_message
		, b.dt_lu
from	email_service_queue a
		join email_service_queue_status b
		on(b.c_email_queue_id=a.c_email_queue_id and b.c_status_id='EMAILSTATUS' and b.c_statusvalue_id='QUEUED')
