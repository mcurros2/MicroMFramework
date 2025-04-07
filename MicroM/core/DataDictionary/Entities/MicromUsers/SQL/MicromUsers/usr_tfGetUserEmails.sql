create or alter function usr_tfGetUserEmails()
returns table as
return 
(

	select	c_user_id = rtrim(a.c_user_id)
			, a.vc_username
			, a.vc_email
	from	microm_users a
	-- union
	-- add the needed unions to get the emails to send the password recovery email

)