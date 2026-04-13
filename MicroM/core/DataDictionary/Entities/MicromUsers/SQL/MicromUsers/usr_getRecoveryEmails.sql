create or alter proc [dbo].usr_GetRecoveryEmails @username varchar(255)
as

select	*
from	[dbo].usr_tfGetUserEmails()
where	vc_username = @username
		and vc_email is not null
