create or alter proc [dbo].mak_get
        @application_id Char(20)
        , @api_key_id Char(20)
        as

select  [c_application_id] = rtrim(a.c_application_id)
        , [c_api_key_id] = rtrim(a.c_api_key_id)
        , a.vc_apikey
        , a.vc_secret
        , b_change_secret=0 /* fake column b_change_secret */
        , a.dt_inserttime
        , a.dt_lu
        , a.vc_webinsuser
        , a.vc_webluuser
        , a.vc_insuser
        , a.vc_luuser
from    [dbo].microm_application_api_keys a
where   a.c_application_id = @application_id
        and a.c_api_key_id = @api_key_id