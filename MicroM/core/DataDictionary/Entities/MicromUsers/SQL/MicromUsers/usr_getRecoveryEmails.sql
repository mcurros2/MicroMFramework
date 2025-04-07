create or alter proc usr_GetRecoveryEmails @username varchar(255)
as

select	*
from	usr_tfGetUserEmails()
where	vc_username = @username
		and vc_email is not null
