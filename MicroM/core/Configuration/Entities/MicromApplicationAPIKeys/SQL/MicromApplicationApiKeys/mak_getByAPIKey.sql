create or alter proc [dbo].mak_getByAPIKey
		@application_id char(20),
		@apikey varchar(2048)
as

declare @api_key_id char(20)

select	@api_key_id = a.c_api_key_id
from	[dbo].microm_application_api_keys a
where	a.c_application_id = @application_id
		and vc_apikey = @apikey

exec [dbo].mak_get @application_id, @api_key_id