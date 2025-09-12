create or alter proc mak_getByAPIKey
		@application_id char(20),
		@apikey varchar(2048)
as

declare @api_key_id char(20)

select	@api_key_id = a.c_api_key_id
from	microm_application_api_keys a
where	a.c_application_id = @application_id
		and vc_apikey = @apikey

exec mak_get @application_id, @api_key_id